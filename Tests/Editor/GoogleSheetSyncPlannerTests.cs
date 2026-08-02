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
        public async Task SaveCoreAsync_MarksRemovedRowsAndBatchesValueClear()
        {
            var current = ManagedSnapshot(
                new[] { "#NAME", "ID", "Value" },
                new[] { "#TYPE", "ID", "string" },
                new[] { "Old", "old", "before" });
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

            Assert.That(client.BatchUpdateCallCount, Is.Zero);
            Assert.That(client.BatchWriteCallCount, Is.EqualTo(1));
            Assert.That(client.BatchClearCallCount, Is.EqualTo(1));
            Assert.That(client.ClearedRanges, Is.EqualTo(new[] { "'Items_item'!C3:C3" }));
            Assert.That(client.WrittenRanges.Any(range =>
                range.Range == "'Items_item'!A3"
                && Convert.ToString(range.Values[0][0]) == GoogleSheetConfig.REMOVED_ROW_MARKER), Is.True);
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
    }
}
