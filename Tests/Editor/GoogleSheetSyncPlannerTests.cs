using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4.Data;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Utils.Sheets;
using TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class GoogleSheetSyncPlannerTests
    {
        [Test]
        public void RemovedManagedField_ProducesPhysicalColumnDelete()
        {
            var current = Snapshot(
                new[] { "#NAME", "ID", "Keep", "Old" },
                new[] { "#TYPE", "ID", "string", "int" });
            var desired = Sheet(
                new[] { "#NAME", "ID", "Keep" },
                new[] { "#TYPE", "ID", "string" });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current);

            var deletion = plan.Operations.Single(operation => operation.Kind == GoogleSheetColumnOperationKind.Delete);
            Assert.That(deletion.FromIndex, Is.EqualTo(3));
            Assert.That(deletion.DisplayName, Is.EqualTo("Old"));
            Assert.That(plan.DeletedColumnNames, Is.EqualTo(new[] { "Old" }));
        }

        [Test]
        public void LegacyRemovedMarker_IsMovedOutThenPhysicallyDeleted()
        {
            var current = Snapshot(
                new[] { "#NAME", "ID", "#REMOVED Old", "Keep" },
                new[] { "#TYPE", "ID", "", "string" });
            var desired = Sheet(
                new[] { "#NAME", "ID", "Keep" },
                new[] { "#TYPE", "ID", "string" });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current);

            Assert.That(plan.Operations.Any(operation => operation.Kind == GoogleSheetColumnOperationKind.Move), Is.True);
            Assert.That(plan.Operations.Any(operation =>
                operation.Kind == GoogleSheetColumnOperationKind.Delete
                && operation.DisplayName == "Old"), Is.True);
        }

        [Test]
        public void UnmanagedUserColumn_IsPreserved()
        {
            var current = Snapshot(
                new[] { "#NAME", "ID", "Designer Notes", "Keep" },
                new[] { "#TYPE", "ID", "", "string" },
                new[] { "", "row", "do not delete", "value" });
            var desired = Sheet(
                new[] { "#NAME", "ID", "Keep" },
                new[] { "#TYPE", "ID", "string" });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current);

            Assert.That(plan.Operations.Any(operation =>
                operation.Kind == GoogleSheetColumnOperationKind.Delete
                && operation.DisplayName == "Designer Notes"), Is.False);
        }

        [Test]
        public void MatchingMetadata_IsIdempotent()
        {
            var current = Snapshot(
                new[] { "#NAME", "ID", "Keep" },
                new[] { "#TYPE", "ID", "string" });
            current.ColumnKeys[0] = GoogleSheetConfig.SYSTEM_NAME_COLUMN_KEY;
            current.ColumnKeys[1] = GoogleSheetConfig.SYSTEM_ID_COLUMN_KEY;
            current.ColumnKeys[2] = GoogleSheetConfig.FIELD_COLUMN_PREFIX + "Keep";
            var desired = Sheet(
                new[] { "#NAME", "ID", "Keep" },
                new[] { "#TYPE", "ID", "string" });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current);

            Assert.That(plan.Operations, Is.Empty);
            Assert.That(plan.MetadataWrites, Is.Empty);
        }

        [Test]
        public void RenamedField_IsInsertPlusDeleteWithoutStableFieldId()
        {
            var current = Snapshot(
                new[] { "#NAME", "ID", "OldName" },
                new[] { "#TYPE", "ID", "string" });
            var desired = Sheet(
                new[] { "#NAME", "ID", "NewName" },
                new[] { "#TYPE", "ID", "string" });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current);

            Assert.That(plan.Operations.Any(operation => operation.Kind == GoogleSheetColumnOperationKind.Insert), Is.True);
            Assert.That(plan.Operations.Any(operation => operation.Kind == GoogleSheetColumnOperationKind.Delete), Is.True);
        }

        [Test]
        public async Task SaveCoreAsync_BatchesTwentySevenNewTablesAcrossSpreadsheet()
        {
            var desired = Enumerable.Range(0, 27)
                .Select(index => new SheetInfo("Table" + index, "table-" + index)
                {
                    Values = Rows(
                        new[] { "#NAME", "ID", "Value" },
                        new[] { "#TYPE", "ID", "string" },
                        new[] { "Row", "row-1", "value" })
                })
                .ToList();
            var client = new RecordingGoogleSheetsApiClient
            {
                VerificationResults = desired.Select(sheet => sheet.Values.Take(2).ToList()).ToList()
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                desired,
                null,
                CancellationToken.None,
                client,
                "spreadsheet-id");

            Assert.That(client.CreateSheetsCallCount, Is.EqualTo(1));
            Assert.That(client.CreatedTitles, Has.Count.EqualTo(27));
            Assert.That(client.BatchUpdateCallCount, Is.EqualTo(1));
            Assert.That(client.BatchWriteCallCount, Is.EqualTo(1));
            Assert.That(client.BatchClearCallCount, Is.Zero);
            Assert.That(client.BatchReadCallCount, Is.EqualTo(1));
            Assert.That(client.WrittenRanges, Has.Count.EqualTo(54));
            Assert.That(client.VerificationRanges, Has.Count.EqualTo(27));
        }

        [Test]
        public async Task SaveCoreAsync_CopiesExistingRowFormatForAllNewRowsInOneRequest()
        {
            var current = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" },
                new[] { "Old", "old", "before" });
            var desired = new SheetInfo("Items", "item")
            {
                Values = Rows(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" },
                    new[] { "Old", "old", "after" },
                    new[] { "New A", "new-a", "a" },
                    new[] { "New B", "new-b", "b" })
            };
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired },
                null,
                CancellationToken.None,
                client,
                "spreadsheet-id");

            var formatCopies = client.StructuralRequests
                .Where(request => request.CopyPaste != null)
                .ToList();
            Assert.That(formatCopies, Has.Count.EqualTo(1));
            Assert.That(formatCopies[0].CopyPaste.PasteType, Is.EqualTo("PASTE_FORMAT"));
            Assert.That(formatCopies[0].CopyPaste.Destination.StartRowIndex, Is.EqualTo(3));
            Assert.That(formatCopies[0].CopyPaste.Destination.EndRowIndex, Is.EqualTo(5));
        }

        [Test]
        public async Task SaveCoreAsync_DeletesRemovedRowsPhysically()
        {
            var current = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" },
                new[] { GoogleSheetConfig.REMOVED_ROW_MARKER, "old", "before" });
            var desired = new SheetInfo("Items", "item")
            {
                Values = Rows(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" })
            };
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired },
                null,
                CancellationToken.None,
                client,
                "spreadsheet-id");

            Assert.That(client.BatchUpdateCallCount, Is.EqualTo(1));
            Assert.That(client.BatchWriteCallCount, Is.EqualTo(1));
            Assert.That(client.BatchClearCallCount, Is.Zero);
            var deletion = client.StructuralRequests.Single(request =>
                request.DeleteDimension?.Range?.Dimension == "ROWS");
            Assert.That(deletion.DeleteDimension.Range.StartIndex, Is.EqualTo(2));
            Assert.That(deletion.DeleteDimension.Range.EndIndex, Is.EqualTo(3));
            Assert.That(client.WrittenRanges.Any(range =>
                range.Values != null
                && range.Values.SelectMany(row => row).Any(value =>
                    Convert.ToString(value) == GoogleSheetConfig.REMOVED_ROW_MARKER)), Is.False);
        }

        [Test]
        public async Task SaveCoreAsync_RecalculatesSurvivorRowsAfterPhysicalDeletion()
        {
            var current = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" },
                new[] { "A", "a", "old-a" },
                new[] { "B", "b", "old-b" },
                new[] { "C", "c", "old-c" });
            var desired = new SheetInfo("Items", "item")
            {
                Values = Rows(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" },
                    new[] { "A", "a", "new-a" },
                    new[] { "C", "c", "new-c" })
            };
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id");

            var deletion = client.StructuralRequests.Single(request =>
                request.DeleteDimension?.Range?.Dimension == "ROWS");
            Assert.That(deletion.DeleteDimension.Range.StartIndex, Is.EqualTo(3));
            Assert.That(deletion.DeleteDimension.Range.EndIndex, Is.EqualTo(4));
            Assert.That(client.WrittenRanges.Any(range => range.Range == "'Items_item'!A4:C4"), Is.True);
            Assert.That(client.WrittenRanges.Any(range => range.Range == "'Items_item'!A5:C5"), Is.False);
        }

        [Test]
        public async Task SaveCoreAsync_GroupsContiguousRowDeletesAndOrdersRangesDescending()
        {
            var current = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" },
                new[] { "A", "a", "a" },
                new[] { "B", "b", "b" },
                new[] { "C", "c", "c" },
                new[] { "D", "d", "d" },
                new[] { "E", "e", "e" });
            var desired = new SheetInfo("Items", "item")
            {
                Values = Rows(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" },
                    new[] { "A", "a", "a" },
                    new[] { "C", "c", "c" })
            };
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id");

            var deletions = client.StructuralRequests
                .Where(request => request.DeleteDimension?.Range?.Dimension == "ROWS")
                .Select(request => request.DeleteDimension.Range)
                .ToList();
            Assert.That(deletions, Has.Count.EqualTo(2));
            Assert.That(deletions[0].StartIndex, Is.EqualTo(5));
            Assert.That(deletions[0].EndIndex, Is.EqualTo(7));
            Assert.That(deletions[1].StartIndex, Is.EqualTo(3));
            Assert.That(deletions[1].EndIndex, Is.EqualTo(4));
        }

        [Test]
        public async Task SaveCoreAsync_PreservesCommentRowsWhileDeletingManagedRows()
        {
            var current = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" },
                new[] { "A", "a", "a" },
                new[] { "# Designer note", "note", "keep this row" },
                new[] { "B", "b", "b" });
            var desired = new SheetInfo("Items", "item")
            {
                Values = Rows(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" },
                    new[] { "A", "a", "a" })
            };
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id");

            var deletion = client.StructuralRequests.Single(request =>
                request.DeleteDimension?.Range?.Dimension == "ROWS");
            Assert.That(deletion.DeleteDimension.Range.StartIndex, Is.EqualTo(4));
            Assert.That(deletion.DeleteDimension.Range.EndIndex, Is.EqualTo(5));
        }

        [Test]
        public async Task SaveCoreAsync_RestoresLegacyRemovedRowWhenIdReturns()
        {
            var current = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" },
                new[] { GoogleSheetConfig.REMOVED_ROW_MARKER, "old", "" });
            var desired = new SheetInfo("Items", "item")
            {
                Values = Rows(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" },
                    new[] { "Restored", "old", "value" })
            };
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id");

            Assert.That(client.StructuralRequests.Any(request =>
                request.DeleteDimension?.Range?.Dimension == "ROWS"), Is.False);
            Assert.That(client.WrittenRanges.Any(range => range.Range == "'Items_item'!A3:C3"), Is.True);
        }

        [Test]
        public async Task SaveCoreAsync_UsesBoundSheetIdAfterSheetRename()
        {
            var current = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" },
                new[] { "A", "a", "old" });
            current.Title = "Renamed By Designer";
            var desired = new SheetInfo("Items", "item")
            {
                Values = Rows(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" },
                    new[] { "A", "a", "new" })
            };
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };
            var bindings = new RecordingBindingStore();
            bindings.Upsert("spreadsheet-id", "item", current.SheetId, "Old Title");

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id", bindings);

            Assert.That(client.CreateSheetsCallCount, Is.Zero);
            Assert.That(client.WrittenRanges.All(range =>
                range.Range.StartsWith("'Renamed By Designer'!", StringComparison.Ordinal)), Is.True);
            Assert.That(bindings.Get("spreadsheet-id", "item").lastKnownTitle, Is.EqualTo("Renamed By Designer"));
        }

        [Test]
        public async Task SaveCoreAsync_AdoptsMetadataWhenLocalBindingIsMissing()
        {
            var current = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" });
            var desired = new SheetInfo("Items", "item")
            {
                Values = Rows(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" })
            };
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };
            var bindings = new RecordingBindingStore();

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id", bindings);

            Assert.That(bindings.Get("spreadsheet-id", "item").sheetId, Is.EqualTo(current.SheetId));
            Assert.That(client.CreateSheetsCallCount, Is.Zero);
        }

        [Test]
        public async Task SaveCoreAsync_ReplacesMissingBoundSheetWithNewSheet()
        {
            var desired = new SheetInfo("Items", "item")
            {
                Values = Rows(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" })
            };
            var client = new RecordingGoogleSheetsApiClient
            {
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };
            var bindings = new RecordingBindingStore();
            bindings.Upsert("spreadsheet-id", "item", 999, "Deleted Sheet");

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id", bindings);

            Assert.That(client.CreateSheetsCallCount, Is.EqualTo(1));
            Assert.That(bindings.Get("spreadsheet-id", "item").sheetId, Is.EqualTo(1000));
        }

        [Test]
        public void SaveCoreAsync_RejectsBindingThatConflictsWithRemoteMetadata()
        {
            var bound = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" });
            bound.HasTableMetadata = false;
            bound.TableId = string.Empty;
            var metadata = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" });
            metadata.SheetId = 11;
            metadata.Title = "Metadata Sheet";
            var desired = new SheetInfo("Items", "item")
            {
                Values = Rows(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" })
            };
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { bound, metadata }
            };
            var bindings = new RecordingBindingStore();
            bindings.Upsert("spreadsheet-id", "item", bound.SheetId, bound.Title);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await GoogleSheetsSyncService.SaveCoreAsync(
                    new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id", bindings));
        }

        private static GoogleSheetSnapshot Snapshot(params string[][] rows)
        {
            return new GoogleSheetSnapshot
            {
                SheetId = 10,
                Title = "Items_item",
                ColumnCount = rows.Max(row => row.Length),
                Values = rows.Select(row => row.ToList()).ToList()
            };
        }

        private static SheetInfo Sheet(params string[][] rows)
        {
            return new SheetInfo("Items", "item")
            {
                Values = rows.Select(row => row.ToList()).ToList()
            };
        }

        private static GoogleSheetSnapshot ManagedSnapshot(params string[][] rows)
        {
            var snapshot = Snapshot(rows);
            snapshot.HasTableMetadata = true;
            snapshot.HasSchemaVersionMetadata = true;
            snapshot.TableId = "item";
            snapshot.RowCount = 1000;
            snapshot.ColumnKeys[0] = GoogleSheetConfig.SYSTEM_NAME_COLUMN_KEY;
            snapshot.ColumnKeys[1] = GoogleSheetConfig.SYSTEM_ID_COLUMN_KEY;
            snapshot.ColumnKeys[2] = GoogleSheetConfig.FIELD_COLUMN_PREFIX + "Value";
            return snapshot;
        }

        private static List<List<string>> Rows(params string[][] rows)
        {
            return rows.Select(row => row.ToList()).ToList();
        }

        private sealed class RecordingGoogleSheetsApiClient : IGoogleSheetsApiClient
        {
            public List<GoogleSheetSnapshot> Snapshots { get; set; } = new List<GoogleSheetSnapshot>();
            public List<List<List<string>>> VerificationResults { get; set; } = new List<List<List<string>>>();
            public List<string> CreatedTitles { get; } = new List<string>();
            public List<Request> StructuralRequests { get; } = new List<Request>();
            public List<ValueRange> WrittenRanges { get; } = new List<ValueRange>();
            public List<string> ClearedRanges { get; } = new List<string>();
            public List<string> VerificationRanges { get; } = new List<string>();
            public int CreateSheetsCallCount { get; private set; }
            public int BatchUpdateCallCount { get; private set; }
            public int BatchWriteCallCount { get; private set; }
            public int BatchClearCallCount { get; private set; }
            public int BatchReadCallCount { get; private set; }

            public Task<List<GoogleSheetSnapshot>> ReadSpreadsheetAsync(string spreadsheetId, CancellationToken ct)
            {
                return Task.FromResult(Snapshots);
            }

            public Task<List<GoogleSheetSnapshot>> CreateSheetsAsync(
                string spreadsheetId,
                IReadOnlyList<string> titles,
                CancellationToken ct)
            {
                CreateSheetsCallCount++;
                CreatedTitles.AddRange(titles);
                return Task.FromResult(titles.Select((title, index) => new GoogleSheetSnapshot
                {
                    SheetId = 1000 + index,
                    Title = title,
                    ColumnCount = 26,
                    RowCount = 1000,
                    Values = new List<List<string>>()
                }).ToList());
            }

            public Task<BatchUpdateSpreadsheetResponse> BatchUpdateAsync(
                string spreadsheetId,
                BatchUpdateSpreadsheetRequest body,
                CancellationToken ct)
            {
                BatchUpdateCallCount++;
                if (body?.Requests != null)
                    StructuralRequests.AddRange(body.Requests);
                return Task.FromResult(new BatchUpdateSpreadsheetResponse());
            }

            public Task BatchWriteValuesAsync(
                string spreadsheetId,
                IReadOnlyList<ValueRange> ranges,
                CancellationToken ct)
            {
                BatchWriteCallCount++;
                WrittenRanges.AddRange(ranges);
                return Task.CompletedTask;
            }

            public Task BatchClearValuesAsync(
                string spreadsheetId,
                IReadOnlyList<string> ranges,
                CancellationToken ct)
            {
                BatchClearCallCount++;
                ClearedRanges.AddRange(ranges);
                return Task.CompletedTask;
            }

            public Task<List<List<List<string>>>> ReadRangesAsync(
                string spreadsheetId,
                IReadOnlyList<string> ranges,
                CancellationToken ct)
            {
                BatchReadCallCount++;
                VerificationRanges.AddRange(ranges);
                return Task.FromResult(VerificationResults);
            }

            public void Dispose()
            {
            }
        }

        private sealed class RecordingBindingStore : IGoogleSheetsBindingStore
        {
            private readonly Dictionary<string, GoogleSheetBinding> _bindings =
                new Dictionary<string, GoogleSheetBinding>(StringComparer.Ordinal);

            public bool TryGet(string spreadsheetId, string tableId, out GoogleSheetBinding binding)
            {
                if (_bindings.TryGetValue(Key(spreadsheetId, tableId), out var found))
                {
                    binding = found.Clone();
                    return true;
                }

                binding = null;
                return false;
            }

            public void Upsert(string spreadsheetId, string tableId, int sheetId, string lastKnownTitle)
            {
                _bindings[Key(spreadsheetId, tableId)] = new GoogleSheetBinding
                {
                    tableId = tableId,
                    sheetId = sheetId,
                    lastKnownTitle = lastKnownTitle
                };
            }

            public GoogleSheetBinding Get(string spreadsheetId, string tableId)
            {
                return _bindings[Key(spreadsheetId, tableId)];
            }

            private static string Key(string spreadsheetId, string tableId)
            {
                return spreadsheetId + "\n" + tableId;
            }
        }
    }
}
