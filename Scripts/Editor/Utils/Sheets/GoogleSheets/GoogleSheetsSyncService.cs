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
                await SaveCoreAsync(
                    desiredSheets,
                    progress,
                    ct,
                    client,
                    spreadsheetId,
                    GoogleSheetsBindingStore.LoadDefault());
            }
        }

        internal static async Task SaveCoreAsync(
            IReadOnlyList<SheetInfo> desiredSheets,
            IProgress<float> progress,
            CancellationToken ct,
            IGoogleSheetsApiClient client,
            string spreadsheetId,
            IGoogleSheetsBindingStore bindingStore = null)
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
                var current = FindSnapshot(
                    snapshots,
                    desired.ID,
                    spreadsheetId,
                    bindingStore);
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
                    bindingStore?.Upsert(
                        spreadsheetId,
                        missing[index].Desired.ID,
                        missing[index].Current.SheetId,
                        missing[index].Current.Title);
                    missing[index].ColumnPlan = GoogleSheetSyncPlanner.BuildColumnPlan(
                        missing[index].Desired,
                        missing[index].Current);
                }
            }

            var structuralRequests = new List<Request>();
            var writes = new List<ValueRange>();
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

            foreach (var deletion in valuePlan.RowDeletions)
            {
                requests.Add(new Request
                {
                    DeleteDimension = new DeleteDimensionRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = sheetId,
                            Dimension = "ROWS",
                            StartIndex = deletion.StartRowNumber - 1,
                            EndIndex = deletion.EndRowNumber
                        }
                    }
                });
            }

            if (valuePlan.RequiredRowCount > valuePlan.ProjectedGridRowCount)
            {
                requests.Add(new Request
                {
                    AppendDimension = new AppendDimensionRequest
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        Length = valuePlan.RequiredRowCount - valuePlan.ProjectedGridRowCount
                    }
                });
            }

            if (valuePlan.HasFormatSourceRow && valuePlan.NewRowNumbers.Count > 0)
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

            var incomingIds = new HashSet<string>(StringComparer.Ordinal);
            var incomingRows = new List<IncomingRow>();

            for (var rowIndex = 2; rowIndex < desired.Values.Count; rowIndex++)
            {
                var row = desired.Values[rowIndex];
                var rowId = GetCell(row, 1);
                if (string.IsNullOrEmpty(rowId))
                    continue;
                if (!incomingIds.Add(rowId))
                    throw new InvalidOperationException($"Sheet '{desired.Name}' contains duplicate row ID '{rowId}'.");

                incomingRows.Add(new IncomingRow { Row = row, RowId = rowId });
            }

            var existingRows = ReadExistingRows(current.Values);
            var deletedRowNumbers = existingRows
                .Where(pair => !incomingIds.Contains(pair.Key))
                .Select(pair => pair.Value)
                .OrderBy(rowNumber => rowNumber)
                .ToList();
            plan.RowDeletions.AddRange(BuildRowDeletionRanges(deletedRowNumbers));

            var projectedUsedRowCount = Math.Max(
                2,
                (current.Values?.Count ?? 0) - deletedRowNumbers.Count);
            var nextRowNumber = Math.Max(projectedUsedRowCount + 1, 3);
            plan.HasFormatSourceRow = projectedUsedRowCount >= 3;

            foreach (var incoming in incomingRows)
            {
                var row = incoming.Row;
                var rowId = incoming.RowId;

                if (!existingRows.TryGetValue(rowId, out var targetRowNumber))
                {
                    targetRowNumber = nextRowNumber++;
                    plan.NewRowNumbers.Add(targetRowNumber);
                }
                else
                {
                    targetRowNumber -= deletedRowNumbers.Count(deleted => deleted < targetRowNumber);
                }

                plan.Writes.Add(SingleRowRange(
                    quotedTitle,
                    targetRowNumber,
                    lastColumn,
                    GetPaddedRow(row, managedColumnCount)));
            }

            plan.RequiredRowCount = Math.Max(2, nextRowNumber - 1);
            plan.ProjectedGridRowCount = Math.Max(1, current.RowCount - deletedRowNumbers.Count);

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
                else
                    throw new InvalidOperationException($"Google Sheet contains duplicate row ID '{rowId}'.");
            }
            return result;
        }

        private static IEnumerable<RowDeletionRange> BuildRowDeletionRanges(IReadOnlyList<int> sortedRowNumbers)
        {
            if (sortedRowNumbers == null || sortedRowNumbers.Count == 0)
                yield break;

            var ranges = new List<RowDeletionRange>();
            var start = sortedRowNumbers[0];
            var end = start;
            for (var index = 1; index < sortedRowNumbers.Count; index++)
            {
                var rowNumber = sortedRowNumbers[index];
                if (rowNumber == end + 1)
                {
                    end = rowNumber;
                    continue;
                }

                ranges.Add(new RowDeletionRange(start, end));
                start = rowNumber;
                end = rowNumber;
            }
            ranges.Add(new RowDeletionRange(start, end));

            for (var index = ranges.Count - 1; index >= 0; index--)
                yield return ranges[index];
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

        private static GoogleSheetSnapshot FindSnapshot(
            IReadOnlyList<GoogleSheetSnapshot> snapshots,
            string tableId,
            string spreadsheetId,
            IGoogleSheetsBindingStore bindingStore)
        {
            var metadataMatches = snapshots
                .Where(item => item.HasTableMetadata && item.TableId == tableId)
                .ToList();
            if (metadataMatches.Count > 1)
                throw new InvalidOperationException($"Multiple Google Sheet tabs contain metadata for ODDB table ID '{tableId}'.");

            if (bindingStore != null
                && bindingStore.TryGet(spreadsheetId, tableId, out var binding))
            {
                var bound = snapshots.FirstOrDefault(item => item.SheetId == binding.sheetId);
                if (bound != null)
                {
                    if (bound.HasTableMetadata
                        && !string.Equals(bound.TableId, tableId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Local binding for ODDB table '{tableId}' points to Google Sheet ID '{bound.SheetId}', " +
                            $"but that sheet belongs to table '{bound.TableId}'.");
                    }

                    if (metadataMatches.Count == 1 && metadataMatches[0].SheetId != bound.SheetId)
                    {
                        throw new InvalidOperationException(
                            $"ODDB table '{tableId}' is locally bound to Google Sheet ID '{bound.SheetId}', " +
                            $"but spreadsheet metadata points to Sheet ID '{metadataMatches[0].SheetId}'.");
                    }

                    bindingStore.Upsert(spreadsheetId, tableId, bound.SheetId, bound.Title);
                    return bound;
                }
            }

            if (metadataMatches.Count == 1)
            {
                var metadataMatch = metadataMatches[0];
                bindingStore?.Upsert(spreadsheetId, tableId, metadataMatch.SheetId, metadataMatch.Title);
                return metadataMatch;
            }

            var legacy = snapshots.FirstOrDefault(item =>
                !item.HasTableMetadata
                && (item.TableId == tableId
                    || item.Title.EndsWith("_" + tableId, StringComparison.Ordinal))
                && IsOddbSheet(item));
            if (legacy != null)
                bindingStore?.Upsert(spreadsheetId, tableId, legacy.SheetId, legacy.Title);
            return legacy;
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
            public List<RowDeletionRange> RowDeletions { get; } = new List<RowDeletionRange>();
            public List<int> NewRowNumbers { get; } = new List<int>();
            public int RequiredRowCount { get; set; }
            public int ProjectedGridRowCount { get; set; }
            public bool HasFormatSourceRow { get; set; }
        }

        private sealed class IncomingRow
        {
            public List<string> Row;
            public string RowId;
        }

        private sealed class RowDeletionRange
        {
            public RowDeletionRange(int startRowNumber, int endRowNumber)
            {
                StartRowNumber = startRowNumber;
                EndRowNumber = endRowNumber;
            }

            public int StartRowNumber { get; }
            public int EndRowNumber { get; }
        }

        private sealed class HeaderVerification
        {
            public SheetInfo Desired;
            public string Title;
            public int ManagedColumnCount;
        }
    }
}
