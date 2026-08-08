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
        public void GroupedSheetWiderSchema_InsertsBeforeTrailingUserColumn()
        {
            var currentPhysical = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[] { Sheet(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" },
                    new[] { "", "weapon", "Sword", "", "Designer note" }) });
            var desired = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[] { Sheet(
                    new[] { "#NAME", "ID", "Value", "Price", "Grade" },
                    new[] { "#TYPE", "ID", "string", "int", "string" }) });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, GroupedSnapshot(currentPhysical));

            var insertion = plan.Operations.Single(operation =>
                operation.Kind == GoogleSheetColumnOperationKind.Insert);
            Assert.That(insertion.ToIndex, Is.EqualTo(4));
        }

        [Test]
        public void GroupedSheetNarrowerSchema_DeletesManagedBoundaryColumn()
        {
            var currentPhysical = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[] { Sheet(
                    new[] { "#NAME", "ID", "Value", "Price", "Grade" },
                    new[] { "#TYPE", "ID", "string", "int", "string" },
                    new[] { "", "weapon", "Sword", "100", "Rare", "Designer note" }) });
            var desired = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[] { Sheet(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" }) });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, GroupedSnapshot(currentPhysical));

            var deletion = plan.Operations.Single(operation =>
                operation.Kind == GoogleSheetColumnOperationKind.Delete);
            Assert.That(deletion.FromIndex, Is.EqualTo(4));
        }

        [Test]
        public void ApplyCellNotes_MapsGridOffsetsToSheetCoordinates()
        {
            var snapshot = Snapshot(
                new[] { "#NAME", "ID", "Item" },
                new[] { "#TYPE", "ID", "View-ItemData" });
            var response = new Spreadsheet
            {
                Sheets = new List<Google.Apis.Sheets.v4.Data.Sheet>
                {
                    new Google.Apis.Sheets.v4.Data.Sheet
                    {
                        Properties = new SheetProperties { SheetId = snapshot.SheetId },
                        Data = new List<GridData>
                        {
                            new GridData
                            {
                                StartRow = 1,
                                StartColumn = 2,
                                RowData = new List<RowData>
                                {
                                    new RowData
                                    {
                                        Values = new List<CellData>
                                        {
                                            new CellData { Note = "ODDB View ID: item-data-id" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            GoogleSheetsApiClient.ApplyCellNotes(new[] { snapshot }, response);

            Assert.That(
                snapshot.CellNotes[new SheetCellAddress(1, 2)],
                Is.EqualTo("ODDB View ID: item-data-id"));
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

        [Test]
        public async Task SaveCoreAsync_GroupedSheetShrinksManagedRowsAndWritesOneRange()
        {
            var currentPhysical = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[]
                {
                    Sheet(
                        new[] { "#NAME", "ID", "Value" },
                        new[] { "#TYPE", "ID", "string" },
                        new[] { "", "weapon", "Sword" }),
                    new SheetInfo("Currency", "currency")
                    {
                        Values = Rows(
                            new[] { "#NAME", "ID", "Value" },
                            new[] { "#TYPE", "ID", "string" },
                            new[] { "", "gold", "Gold" })
                    }
                });
            var desired = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[] { Sheet(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" },
                    new[] { "", "weapon", "Sword" }) });
            var current = GroupedSnapshot(currentPhysical);
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id");

            var deletion = client.StructuralRequests.Single(request =>
                request.DeleteDimension?.Range?.Dimension == "ROWS");
            Assert.That(deletion.DeleteDimension.Range.StartIndex, Is.EqualTo(desired.Values.Count));
            Assert.That(deletion.DeleteDimension.Range.EndIndex, Is.EqualTo(currentPhysical.Values.Count));
            Assert.That(client.WrittenRanges, Has.Count.EqualTo(1));
            Assert.That(client.WrittenRanges[0].Range, Does.EndWith(desired.Values.Count.ToString()));
        }

        [Test]
        public async Task SaveCoreAsync_GroupedSheetGrowsBeforeTrailingUserRows()
        {
            var currentPhysical = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[] { Sheet(
                    new[] { "#NAME", "ID", "Value" },
                    new[] { "#TYPE", "ID", "string" }) });
            var desired = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[]
                {
                    Sheet(
                        new[] { "#NAME", "ID", "Value" },
                        new[] { "#TYPE", "ID", "string" }),
                    new SheetInfo("Currency", "currency")
                    {
                        Values = Rows(
                            new[] { "#NAME", "ID", "Value" },
                            new[] { "#TYPE", "ID", "string" })
                    }
                });
            var current = GroupedSnapshot(currentPhysical);
            current.Values.Add(new List<string> { "Designer notes below the managed group" });
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id");

            var insertion = client.StructuralRequests.Single(request =>
                request.InsertDimension?.Range?.Dimension == "ROWS");
            Assert.That(insertion.InsertDimension.Range.StartIndex, Is.EqualTo(currentPhysical.Values.Count));
            Assert.That(
                insertion.InsertDimension.Range.EndIndex - insertion.InsertDimension.Range.StartIndex,
                Is.EqualTo(desired.Values.Count - currentPhysical.Values.Count));
            Assert.That(client.WrittenRanges, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task SaveCoreAsync_FormatsGroupedMarkerRows()
        {
            var desired = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[]
                {
                    new SheetInfo("Weapon", "weapon-item")
                    {
                        Values = Rows(
                            new[] { "#NAME", "ID", "Value" },
                            new[] { "#TYPE", "ID", "string" },
                            new[] { "", "sword", "Sword" })
                    }
                });
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { GroupedSnapshot(desired) },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id");

            var formats = client.StructuralRequests
                .Where(request => Convert.ToString(request.RepeatCell?.Fields)
                    .Contains("userEnteredFormat"))
                .Select(request => request.RepeatCell)
                .ToList();
            Assert.That(formats, Has.Count.EqualTo(4));

            var group = formats.Single(format => format.Range.StartRowIndex == 0);
            Assert.That(group.Range.EndRowIndex, Is.EqualTo(1));
            AssertColor(group.Cell.UserEnteredFormat.BackgroundColorStyle.RgbColor, 0.29f, 0.64f, 0.89f);

            var metadata = formats.Single(format => format.Range.StartRowIndex == 1);
            Assert.That(metadata.Range.EndRowIndex, Is.EqualTo(4));
            AssertColor(metadata.Cell.UserEnteredFormat.BackgroundColorStyle.RgbColor, 0.38f, 0.41f, 0.46f);

            var end = formats.Single(format => format.Range.StartRowIndex == 5);
            Assert.That(end.Range.EndRowIndex, Is.EqualTo(6));
            AssertColor(end.Cell.UserEnteredFormat.BackgroundColorStyle.RgbColor, 0.79f, 0.42f, 0.42f);

            var groupEnd = formats.Single(format => format.Range.StartRowIndex == 6);
            Assert.That(groupEnd.Range.EndRowIndex, Is.EqualTo(7));
            AssertColor(groupEnd.Cell.UserEnteredFormat.BackgroundColorStyle.RgbColor, 0.29f, 0.64f, 0.89f);

            foreach (var format in formats)
            {
                Assert.That(format.Range.StartColumnIndex, Is.EqualTo(0));
                Assert.That(format.Range.EndColumnIndex, Is.EqualTo(4));
                Assert.That(format.Cell.UserEnteredFormat.TextFormat.Bold, Is.True);
                AssertColor(
                    format.Cell.UserEnteredFormat.TextFormat.ForegroundColor,
                    1f,
                    1f,
                    1f);
            }
        }

        [Test]
        public async Task SaveCoreAsync_ReparentedTableIsRemovedFromPreviousGroup()
        {
            var oldPhysical = GroupedSheetCodec.Pack(
                "OldRoot",
                "old-root",
                new[]
                {
                    new SheetInfo("Weapon", "weapon-item")
                    {
                        Values = Rows(
                            new[] { "#NAME", "ID", "Value" },
                            new[] { "#TYPE", "ID", "string" },
                            new[] { "", "sword", "Sword" })
                    }
                });
            var newPhysical = GroupedSheetCodec.Pack(
                "NewRoot",
                "new-root",
                new[]
                {
                    new SheetInfo("Weapon", "weapon-item")
                    {
                        Values = Rows(
                            new[] { "#NAME", "ID", "Value" },
                            new[] { "#TYPE", "ID", "string" },
                            new[] { "", "sword", "Sword" })
                    }
                });
            var oldSnapshot = GroupedSnapshot(oldPhysical);
            oldSnapshot.TableId = "old-root";
            oldSnapshot.Title = "OldRoot_old-root";
            var emptyOld = GroupedSheetCodec.Pack("OldRoot", "old-root", Array.Empty<SheetInfo>());
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { oldSnapshot },
                VerificationResults = new List<List<List<string>>>
                {
                    newPhysical.Values.Take(2).ToList(),
                    emptyOld.Values.Take(2).ToList()
                }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { newPhysical },
                null,
                CancellationToken.None,
                client,
                "spreadsheet-id",
                null,
                true);

            Assert.That(client.CreateSheetsCallCount, Is.EqualTo(1));
            Assert.That(client.WrittenRanges, Has.Count.EqualTo(2));
            var oldGroupWrite = client.WrittenRanges.Single(range =>
                range.Range.StartsWith("'OldRoot_old-root'!", StringComparison.Ordinal));
            Assert.That(oldGroupWrite.Values.SelectMany(row => row).Any(value =>
                string.Equals(Convert.ToString(value), "weapon-item", StringComparison.Ordinal)), Is.False);
        }

        [Test]
        public async Task SaveCoreAsync_GroupMarkerRecoversRenamedTabWithoutMetadataOrBinding()
        {
            var desired = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[]
                {
                    new SheetInfo("Weapon", "weapon-item")
                    {
                        Values = Rows(
                            new[] { "#NAME", "ID", "Value" },
                            new[] { "#TYPE", "ID", "string" })
                    }
                });
            var current = GroupedSnapshot(desired);
            current.Title = "Renamed by Designer";
            current.TableId = string.Empty;
            current.HasTableMetadata = false;
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id");

            Assert.That(client.CreateSheetsCallCount, Is.Zero);
            Assert.That(client.WrittenRanges.Single().Range, Does.StartWith("'Renamed by Designer'!"));
            Assert.That(client.StructuralRequests.Any(request =>
                request.CreateDeveloperMetadata?.DeveloperMetadata?.MetadataKey
                == GoogleSheetConfig.TABLE_ID_METADATA_KEY), Is.True);
        }

        [Test]
        public async Task SaveCoreAsync_WritesStableViewIdAsCellNote()
        {
            var database = new TeamODD.ODDB.Runtime.ODDatabase();
            var referenced = database.Views.Create(
                new TeamODD.ODDB.Runtime.Utils.Converters.ODDBID("item-data-id"));
            referenced.Name = "ItemData";
            var desired = Sheet(
                new[] { "#NAME", "ID", "Item" },
                new[] { "#TYPE", "ID", "view - item-data-id" });
            var client = new RecordingGoogleSheetsApiClient
            {
                VerificationResults = new List<List<List<string>>>
                {
                    Rows(
                        new[] { "#NAME", "ID", "Item" },
                        new[] { "#TYPE", "ID", "View-ItemData" })
                }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired },
                null,
                CancellationToken.None,
                client,
                "spreadsheet-id",
                null,
                false,
                database);

            var noteRequest = client.StructuralRequests.Single(request =>
                request.RepeatCell?.Cell?.Note == "ODDB View ID: item-data-id");
            Assert.That(noteRequest.RepeatCell.Range.StartRowIndex, Is.EqualTo(1));
            Assert.That(noteRequest.RepeatCell.Range.StartColumnIndex, Is.EqualTo(2));
            Assert.That(client.WrittenRanges.Single().Values[1][2], Is.EqualTo("View-ItemData"));
        }

        [Test]
        public async Task SaveCoreAsync_ClearsOnlyOddbOwnedCellNotes()
        {
            var desired = Sheet(
                new[] { "#NAME", "ID", "Item" },
                new[] { "#TYPE", "ID", "string" });
            var current = ManagedSnapshot(
                new[] { "#NAME", "ID", "Item" },
                new[] { "#TYPE", "ID", "View-OldItem" });
            current.ColumnKeys[2] = GoogleSheetConfig.FIELD_COLUMN_PREFIX + "Item";
            current.CellNotes[new SheetCellAddress(1, 2)] = "ODDB View ID: old-item-id";
            current.CellNotes[new SheetCellAddress(1, 3)] = "Designer note";
            var client = new RecordingGoogleSheetsApiClient
            {
                Snapshots = new List<GoogleSheetSnapshot> { current },
                VerificationResults = new List<List<List<string>>> { desired.Values.Take(2).ToList() }
            };

            await GoogleSheetsSyncService.SaveCoreAsync(
                new[] { desired }, null, CancellationToken.None, client, "spreadsheet-id");

            var noteRequests = client.StructuralRequests
                .Where(request => request.RepeatCell != null)
                .ToList();
            Assert.That(noteRequests, Has.Count.EqualTo(1));
            Assert.That(noteRequests[0].RepeatCell.Range.StartColumnIndex, Is.EqualTo(2));
            Assert.That(noteRequests[0].RepeatCell.Cell.Note, Is.Null);
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

        private static GoogleSheetSnapshot GroupedSnapshot(SheetInfo grouped)
        {
            return new GoogleSheetSnapshot
            {
                SheetId = 10,
                Title = "ItemView_item-view",
                ColumnCount = Math.Max(26, GroupedSheetCodec.GetManagedColumnCount(grouped)),
                RowCount = 1000,
                TableId = grouped.ID,
                HasTableMetadata = true,
                HasSchemaVersionMetadata = true,
                Values = grouped.Values.Select(row => new List<string>(row)).ToList()
            };
        }

        private static List<List<string>> Rows(params string[][] rows)
        {
            return rows.Select(row => row.ToList()).ToList();
        }

        private static void AssertColor(Color color, float red, float green, float blue)
        {
            Assert.That(color, Is.Not.Null);
            Assert.That(color.Red, Is.EqualTo(red).Within(0.001f));
            Assert.That(color.Green, Is.EqualTo(green).Within(0.001f));
            Assert.That(color.Blue, Is.EqualTo(blue).Within(0.001f));
            Assert.That(color.Alpha, Is.EqualTo(1f).Within(0.001f));
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

            public bool TryGet(string spreadsheetId, string sheetKey, out GoogleSheetBinding binding)
            {
                if (_bindings.TryGetValue(Key(spreadsheetId, sheetKey), out var found))
                {
                    binding = found.Clone();
                    return true;
                }

                binding = null;
                return false;
            }

            public void Upsert(string spreadsheetId, string sheetKey, int sheetId, string lastKnownTitle)
            {
                _bindings[Key(spreadsheetId, sheetKey)] = new GoogleSheetBinding
                {
                    sheetKey = sheetKey,
                    sheetId = sheetId,
                    lastKnownTitle = lastKnownTitle
                };
            }

            public GoogleSheetBinding Get(string spreadsheetId, string sheetKey)
            {
                return _bindings[Key(spreadsheetId, sheetKey)];
            }

            private static string Key(string spreadsheetId, string sheetKey)
            {
                return spreadsheetId + "\n" + sheetKey;
            }
        }
    }
}
