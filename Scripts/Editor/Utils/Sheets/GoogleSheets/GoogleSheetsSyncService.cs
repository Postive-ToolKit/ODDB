using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4.Data;
using TeamODD.ODDB.Editors.Settings;
using TeamODD.ODDB.Runtime;
using UnityEditor;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal sealed class GoogleSheetsSyncService
    {
        public async Task<List<SheetInfo>> LoadAsync(ExportScope scope, CancellationToken ct)
        {
            var spreadsheetId = GetSpreadsheetId();
            using (var client = await GoogleSheetsApiClient.CreateAsync(ct))
            {
                var snapshots = await client.ReadSpreadsheetAsync(spreadsheetId, ct);
                ResolveLegacyTableIds(snapshots);
                var result = new List<SheetInfo>();
                var seenIds = new HashSet<string>(StringComparer.Ordinal);

                foreach (var snapshot in snapshots)
                {
                    if (!IsOddbSheet(snapshot) || string.IsNullOrEmpty(snapshot.TableId))
                        continue;
                    if (!scope.All && snapshot.TableId != scope.TargetTableId)
                        continue;
                    if (!seenIds.Add(snapshot.TableId))
                        throw new InvalidOperationException($"Multiple Google Sheet tabs map to ODDB table ID '{snapshot.TableId}'.");

                    result.Add(new SheetInfo(GetDisplayName(snapshot.Title, snapshot.TableId), snapshot.TableId)
                    {
                        Values = snapshot.Values
                    });
                }

                return result;
            }
        }

        public async Task SaveAsync(
            IReadOnlyList<SheetInfo> desiredSheets,
            IProgress<float> progress,
            CancellationToken ct)
        {
            if (desiredSheets == null) throw new ArgumentNullException(nameof(desiredSheets));
            var spreadsheetId = GetSpreadsheetId();

            using (var client = await GoogleSheetsApiClient.CreateAsync(ct))
            {
                await SaveCoreAsync(desiredSheets, progress, ct, client, spreadsheetId);
            }
        }

        internal static async Task SaveCoreAsync(
            IReadOnlyList<SheetInfo> desiredSheets,
            IProgress<float> progress,
            CancellationToken ct,
            IGoogleSheetsApiClient client,
            string spreadsheetId)
        {
            if (desiredSheets == null) throw new ArgumentNullException(nameof(desiredSheets));
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrWhiteSpace(spreadsheetId)) throw new ArgumentException("Spreadsheet ID is required.", nameof(spreadsheetId));

            var snapshots = await client.ReadSpreadsheetAsync(spreadsheetId, ct);
            EnsureNoDuplicateTableMetadata(snapshots);

            var pending = new List<PendingSheetSync>();
            var reservedTitles = new List<string>(snapshots.Select(item => item.Title));
            foreach (var desired in desiredSheets.Where(sheet => sheet != null))
            {
                var current = FindSnapshot(snapshots, desired.ID);
                if (current == null)
                {
                    var title = CreateUniqueTitle(desired, reservedTitles);
                    reservedTitles.Add(title);
                    current = new GoogleSheetSnapshot
                    {
                        SheetId = -1,
                        Title = title,
                        ColumnCount = 26,
                        RowCount = 1000,
                        TableId = desired.ID,
                        Values = new List<List<string>>()
                    };
                }

                pending.Add(new PendingSheetSync
                {
                    Desired = desired,
                    Current = current,
                    ColumnPlan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current)
                });
            }

            ConfirmDestructiveChanges(pending);
            var missing = pending.Where(item => item.Current.SheetId < 0).ToList();
            if (missing.Count > 0)
            {
                var created = await client.CreateSheetsAsync(
                    spreadsheetId,
                    missing.Select(item => item.Current.Title).ToList(),
                    ct);
                if (created.Count != missing.Count)
                    throw new InvalidOperationException("Google Sheets did not return every newly created sheet.");

                for (var index = 0; index < missing.Count; index++)
                {
                    missing[index].Current = created[index];
                    missing[index].Current.TableId = missing[index].Desired.ID;
                    missing[index].ColumnPlan = GoogleSheetSyncPlanner.BuildColumnPlan(
                        missing[index].Desired,
                        missing[index].Current);
                }
            }

            var structuralRequests = new List<Request>();
            var writes = new List<ValueRange>();
            var clears = new List<string>();
            var verifications = new List<HeaderVerification>();
            for (var index = 0; index < pending.Count; index++)
            {
                ct.ThrowIfCancellationRequested();
                var item = pending[index];
                var valuePlan = BuildValuePlan(
                    item.Desired,
                    item.Current,
                    item.ColumnPlan.ManagedColumnCount);
                structuralRequests.AddRange(BuildStructuralRequests(item, valuePlan));
                writes.AddRange(valuePlan.Writes);
                clears.AddRange(valuePlan.Clears);
                verifications.Add(new HeaderVerification
                {
                    Desired = item.Desired,
                    Title = item.Current.Title,
                    ManagedColumnCount = item.ColumnPlan.ManagedColumnCount
                });
                progress?.Report(pending.Count == 0 ? 0.4f : 0.4f * (index + 1f) / pending.Count);
            }

            if (structuralRequests.Count > 0)
            {
                await client.BatchUpdateAsync(
                    spreadsheetId,
                    new BatchUpdateSpreadsheetRequest { Requests = structuralRequests },
                    ct);
            }

            progress?.Report(0.55f);
            if (writes.Count > 0)
                await client.BatchWriteValuesAsync(spreadsheetId, writes, ct);
            progress?.Report(0.75f);
            if (clears.Count > 0)
                await client.BatchClearValuesAsync(spreadsheetId, clears, ct);
            progress?.Report(0.85f);
            await VerifyHeadersAsync(client, spreadsheetId, verifications, ct);
            progress?.Report(1f);
        }

        private static async Task VerifyHeadersAsync(
            IGoogleSheetsApiClient client,
            string spreadsheetId,
            IReadOnlyList<HeaderVerification> verifications,
            CancellationToken ct)
        {
            if (verifications == null || verifications.Count == 0)
                return;

            var ranges = verifications.Select(item =>
                $"{GoogleSheetsApiClient.QuoteTitle(item.Title)}!A1:{ToColumnName(item.ManagedColumnCount)}2").ToList();
            var actualHeaders = await client.ReadRangesAsync(spreadsheetId, ranges, ct);
            if (actualHeaders.Count != verifications.Count)
                throw new InvalidOperationException("Google Sheets did not return every header verification range.");

            for (var index = 0; index < verifications.Count; index++)
            {
                var verification = verifications[index];
                var actual = actualHeaders[index];
                for (var row = 0; row < 2; row++)
                {
                    var expectedRow = GetPaddedRow(
                        verification.Desired.Values,
                        row,
                        verification.ManagedColumnCount);
                    for (var column = 0; column < verification.ManagedColumnCount; column++)
                    {
                        if (!string.Equals(GetCell(actual, row, column), expectedRow[column], StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                $"Google Sheet verification failed for '{verification.Title}' at row {row + 1}, column {column + 1}.");
                        }
                    }
                }
            }
        }

        private static List<Request> BuildStructuralRequests(PendingSheetSync pending, ValueSyncPlan valuePlan)
        {
            var requests = new List<Request>();
            var sheetId = pending.Current.SheetId;

            foreach (var operation in pending.ColumnPlan.Operations)
            {
                switch (operation.Kind)
                {
                    case GoogleSheetColumnOperationKind.Insert:
                        requests.Add(new Request
                        {
                            InsertDimension = new InsertDimensionRequest
                            {
                                Range = ColumnRange(sheetId, operation.ToIndex),
                                InheritFromBefore = operation.ToIndex > 0
                            }
                        });
                        break;
                    case GoogleSheetColumnOperationKind.Move:
                        requests.Add(new Request
                        {
                            MoveDimension = new MoveDimensionRequest
                            {
                                Source = ColumnRange(sheetId, operation.FromIndex),
                                DestinationIndex = operation.FromIndex < operation.ToIndex
                                    ? operation.ToIndex + 1
                                    : operation.ToIndex
                            }
                        });
                        break;
                    case GoogleSheetColumnOperationKind.Delete:
                        requests.Add(new Request
                        {
                            DeleteDimension = new DeleteDimensionRequest
                            {
                                Range = ColumnRange(sheetId, operation.FromIndex)
                            }
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (valuePlan.RequiredRowCount > pending.Current.RowCount)
            {
                requests.Add(new Request
                {
                    AppendDimension = new AppendDimensionRequest
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        Length = valuePlan.RequiredRowCount - pending.Current.RowCount
                    }
                });
            }

            if ((pending.Current.Values?.Count ?? 0) >= 3 && valuePlan.NewRowNumbers.Count > 0)
            {
                var firstRowNumber = valuePlan.NewRowNumbers[0];
                var lastRowNumber = valuePlan.NewRowNumbers[valuePlan.NewRowNumbers.Count - 1];
                var targetStart = firstRowNumber - 1;
                requests.Add(new Request
                {
                    CopyPaste = new CopyPasteRequest
                    {
                        Source = new GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = targetStart - 1,
                            EndRowIndex = targetStart,
                            StartColumnIndex = 0,
                            EndColumnIndex = pending.ColumnPlan.ManagedColumnCount
                        },
                        Destination = new GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = targetStart,
                            EndRowIndex = lastRowNumber,
                            StartColumnIndex = 0,
                            EndColumnIndex = pending.ColumnPlan.ManagedColumnCount
                        },
                        PasteType = "PASTE_FORMAT",
                        PasteOrientation = "NORMAL"
                    }
                });
            }

            if (!pending.Current.HasTableMetadata)
                requests.Add(CreateSheetMetadata(sheetId, GoogleSheetConfig.TABLE_ID_METADATA_KEY, pending.Desired.ID));
            if (!pending.Current.HasSchemaVersionMetadata)
                requests.Add(CreateSheetMetadata(sheetId, GoogleSheetConfig.SCHEMA_VERSION_METADATA_KEY, GoogleSheetConfig.SCHEMA_VERSION));

            foreach (var metadata in pending.ColumnPlan.MetadataWrites)
            {
                requests.Add(new Request
                {
                    CreateDeveloperMetadata = new CreateDeveloperMetadataRequest
                    {
                        DeveloperMetadata = new DeveloperMetadata
                        {
                            MetadataKey = GoogleSheetConfig.COLUMN_KEY_METADATA_KEY,
                            MetadataValue = metadata.ColumnKey,
                            Visibility = "DOCUMENT",
                            Location = new DeveloperMetadataLocation
                            {
                                DimensionRange = ColumnRange(sheetId, metadata.ColumnIndex)
                            }
                        }
                    }
                });
            }

            return requests;
        }

        private static ValueSyncPlan BuildValuePlan(
            SheetInfo desired,
            GoogleSheetSnapshot current,
            int managedColumnCount)
        {
            var plan = new ValueSyncPlan();
            var quotedTitle = GoogleSheetsApiClient.QuoteTitle(current.Title);
            var lastColumn = ToColumnName(managedColumnCount);
            var headerRows = new List<IList<object>>
            {
                ToObjectRow(GetPaddedRow(desired.Values, 0, managedColumnCount)),
                ToObjectRow(GetPaddedRow(desired.Values, 1, managedColumnCount))
            };
            plan.Writes.Add(new ValueRange
            {
                Range = $"{quotedTitle}!A1:{lastColumn}2",
                MajorDimension = "ROWS",
                Values = headerRows
            });

            var existingRows = ReadExistingRows(current.Values);
            var incomingIds = new HashSet<string>(StringComparer.Ordinal);
            var nextRowNumber = Math.Max((current.Values?.Count ?? 0) + 1, 3);

            for (var rowIndex = 2; rowIndex < desired.Values.Count; rowIndex++)
            {
                var row = desired.Values[rowIndex];
                var rowId = GetCell(row, 1);
                if (string.IsNullOrEmpty(rowId))
                    continue;
                if (!incomingIds.Add(rowId))
                    throw new InvalidOperationException($"Sheet '{desired.Name}' contains duplicate row ID '{rowId}'.");

                if (!existingRows.TryGetValue(rowId, out var targetRowNumber))
                {
                    targetRowNumber = nextRowNumber++;
                    plan.NewRowNumbers.Add(targetRowNumber);
                }

                plan.Writes.Add(SingleRowRange(
                    quotedTitle,
                    targetRowNumber,
                    lastColumn,
                    GetPaddedRow(row, managedColumnCount)));
            }

            foreach (var pair in existingRows)
            {
                if (incomingIds.Contains(pair.Key))
                    continue;

                plan.Writes.Add(new ValueRange
                {
                    Range = $"{quotedTitle}!A{pair.Value}",
                    MajorDimension = "ROWS",
                    Values = new List<IList<object>>
                    {
                        new List<object> { GoogleSheetConfig.REMOVED_ROW_MARKER }
                    }
                });
                if (managedColumnCount > 2)
                    plan.Clears.Add($"{quotedTitle}!C{pair.Value}:{lastColumn}{pair.Value}");
            }

            plan.RequiredRowCount = Math.Max(2, nextRowNumber - 1);

            return plan;
        }

        private static Dictionary<string, int> ReadExistingRows(IReadOnlyList<List<string>> values)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (values == null)
                return result;

            for (var index = 2; index < values.Count; index++)
            {
                var row = values[index];
                var marker = GetCell(row, 0);
                var rowId = GetCell(row, 1);
                if (string.IsNullOrEmpty(rowId))
                    continue;
                if (!string.IsNullOrEmpty(marker)
                    && marker.StartsWith(SheetConfig.ROW_COMMENT_PREFIX, StringComparison.Ordinal)
                    && marker != GoogleSheetConfig.REMOVED_ROW_MARKER)
                {
                    continue;
                }

                if (!result.ContainsKey(rowId))
                    result.Add(rowId, index + 1);
            }
            return result;
        }

        private static Request CreateSheetMetadata(int sheetId, string key, string value)
        {
            return new Request
            {
                CreateDeveloperMetadata = new CreateDeveloperMetadataRequest
                {
                    DeveloperMetadata = new DeveloperMetadata
                    {
                        MetadataKey = key,
                        MetadataValue = value,
                        Visibility = "DOCUMENT",
                        Location = new DeveloperMetadataLocation { SheetId = sheetId }
                    }
                }
            };
        }

        private static DimensionRange ColumnRange(int sheetId, int index)
        {
            return new DimensionRange
            {
                SheetId = sheetId,
                Dimension = "COLUMNS",
                StartIndex = index,
                EndIndex = index + 1
            };
        }

        private static ValueRange SingleRowRange(string quotedTitle, int rowNumber, string lastColumn, List<string> row)
        {
            return new ValueRange
            {
                Range = $"{quotedTitle}!A{rowNumber}:{lastColumn}{rowNumber}",
                MajorDimension = "ROWS",
                Values = new List<IList<object>> { ToObjectRow(row) }
            };
        }

        private static List<string> GetPaddedRow(IReadOnlyList<List<string>> rows, int rowIndex, int width)
        {
            var source = rows != null && rowIndex >= 0 && rowIndex < rows.Count
                ? rows[rowIndex]
                : null;
            return GetPaddedRow(source, width);
        }

        private static List<string> GetPaddedRow(IReadOnlyList<string> source, int width)
        {
            var result = new List<string>(width);
            for (var index = 0; index < width; index++)
                result.Add(GetCell(source, index));
            return result;
        }

        private static IList<object> ToObjectRow(IEnumerable<string> row)
        {
            return row.Select(value => (object)(value ?? string.Empty)).ToList();
        }

        private static string GetCell(IReadOnlyList<string> row, int index)
        {
            return row != null && index >= 0 && index < row.Count
                ? row[index] ?? string.Empty
                : string.Empty;
        }

        private static string GetCell(IReadOnlyList<List<string>> rows, int rowIndex, int columnIndex)
        {
            return rows != null && rowIndex >= 0 && rowIndex < rows.Count
                ? GetCell(rows[rowIndex], columnIndex)
                : string.Empty;
        }

        private static string ToColumnName(int oneBasedColumn)
        {
            if (oneBasedColumn <= 0) throw new ArgumentOutOfRangeException(nameof(oneBasedColumn));
            var builder = new StringBuilder();
            var value = oneBasedColumn;
            while (value > 0)
            {
                value--;
                builder.Insert(0, (char)('A' + value % 26));
                value /= 26;
            }
            return builder.ToString();
        }

        private static GoogleSheetSnapshot FindSnapshot(IEnumerable<GoogleSheetSnapshot> snapshots, string tableId)
        {
            var metadataMatches = snapshots
                .Where(item => item.HasTableMetadata && item.TableId == tableId)
                .ToList();
            if (metadataMatches.Count > 1)
                throw new InvalidOperationException($"Multiple Google Sheet tabs contain metadata for ODDB table ID '{tableId}'.");
            if (metadataMatches.Count == 1)
                return metadataMatches[0];

            return snapshots.FirstOrDefault(item =>
                !item.HasTableMetadata
                && (item.TableId == tableId
                    || item.Title.EndsWith("_" + tableId, StringComparison.Ordinal))
                && IsOddbSheet(item));
        }

        private static void ResolveLegacyTableIds(IEnumerable<GoogleSheetSnapshot> snapshots)
        {
            var database = ODDBEditorRuntime.UseCase?.DataBase as ODDatabase;
            if (database == null)
                return;

            var knownIds = database.Tables.GetAll()
                .Select(view => view?.ID.ToString())
                .Where(id => !string.IsNullOrEmpty(id))
                .OrderByDescending(id => id.Length)
                .ToList();

            foreach (var snapshot in snapshots)
            {
                if (snapshot.HasTableMetadata || !IsOddbSheet(snapshot))
                    continue;
                var matched = knownIds.FirstOrDefault(id =>
                    snapshot.Title.EndsWith("_" + id, StringComparison.Ordinal));
                if (!string.IsNullOrEmpty(matched))
                    snapshot.TableId = matched;
            }
        }

        private static bool IsOddbSheet(GoogleSheetSnapshot snapshot)
        {
            return snapshot.HasTableMetadata
                   || GetCell(snapshot.Values, 0, 0) == SheetConfig.ROW_NAME_MARKER;
        }

        private static void EnsureNoDuplicateTableMetadata(IEnumerable<GoogleSheetSnapshot> snapshots)
        {
            var duplicate = snapshots
                .Where(item => item.HasTableMetadata && !string.IsNullOrEmpty(item.TableId))
                .GroupBy(item => item.TableId, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new InvalidOperationException($"Multiple Google Sheet tabs contain metadata for ODDB table ID '{duplicate.Key}'.");
        }

        private static void ConfirmDestructiveChanges(IEnumerable<PendingSheetSync> pending)
        {
            if (!ODDBEditorSettings.Setting.ConfirmGoogleSheetColumnDeletion)
                return;

            var changes = pending
                .Where(item => item.ColumnPlan.DeletedColumnNames.Count > 0)
                .ToList();
            if (changes.Count == 0)
                return;

            var message = new StringBuilder();
            message.AppendLine("The following ODDB-managed Google Sheet columns will be permanently deleted:");
            message.AppendLine();
            foreach (var item in changes)
            {
                message.AppendLine(item.Current.Title + ":");
                foreach (var name in item.ColumnPlan.DeletedColumnNames)
                    message.AppendLine("  - " + name);
            }
            message.AppendLine();
            message.AppendLine("Cell contents and formatting in those columns will be removed.");

            if (!EditorUtility.DisplayDialog("ODDB Google Sheets Export", message.ToString(), "Delete and Export", "Cancel"))
                throw new OperationCanceledException("Google Sheets export was cancelled before deleting columns.");
        }

        private static string GetSpreadsheetId()
        {
            var id = ODDBEditorSettings.Setting.GoogleSpreadsheetId?.Trim();
            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException("Google Spreadsheet ID is not configured in ODDBEditorSettings.");
            if (!GoogleSheetsUserSettings.HasStoredAuthorization)
                throw new InvalidOperationException("Sign in with Google from ODDBEditorSettings first.");
            return id;
        }

        private static string CreateUniqueTitle(SheetInfo sheet, IEnumerable<string> currentTitles)
        {
            var existing = new HashSet<string>(currentTitles ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var baseTitle = SanitizeTitle($"{sheet.Name}_{sheet.ID}");
            if (!existing.Contains(baseTitle))
                return baseTitle;
            for (var suffix = 2; suffix < 1000; suffix++)
            {
                var candidate = SanitizeTitle(baseTitle + " (" + suffix + ")");
                if (!existing.Contains(candidate))
                    return candidate;
            }
            throw new InvalidOperationException($"Could not create a unique Google Sheet title for '{sheet.Name}'.");
        }

        private static string SanitizeTitle(string title)
        {
            var invalid = new HashSet<char>(new[] { ':', '\\', '/', '?', '*', '[', ']' });
            var value = new string((title ?? "ODDB").Select(character => invalid.Contains(character) ? '_' : character).ToArray());
            if (value.Length > 100)
                value = value.Substring(0, 100);
            return string.IsNullOrWhiteSpace(value) ? "ODDB" : value;
        }

        private static string GetDisplayName(string title, string tableId)
        {
            var suffix = "_" + tableId;
            return !string.IsNullOrEmpty(title) && title.EndsWith(suffix, StringComparison.Ordinal)
                ? title.Substring(0, title.Length - suffix.Length)
                : title;
        }

        private sealed class PendingSheetSync
        {
            public SheetInfo Desired;
            public GoogleSheetSnapshot Current;
            public GoogleSheetColumnSyncPlan ColumnPlan;
        }

        private sealed class ValueSyncPlan
        {
            public List<ValueRange> Writes { get; } = new List<ValueRange>();
            public List<string> Clears { get; } = new List<string>();
            public List<int> NewRowNumbers { get; } = new List<int>();
            public int RequiredRowCount { get; set; }
        }

        private sealed class HeaderVerification
        {
            public SheetInfo Desired;
            public string Title;
            public int ManagedColumnCount;
        }
    }
}
