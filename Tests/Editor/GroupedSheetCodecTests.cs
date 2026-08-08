using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Utils.Sheets;
using TeamODD.ODDB.Editors.Utils.Sheets.CSV;
using TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets;
using TeamODD.ODDB.Editors.Utils.Sheets.Validation;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class GroupedSheetCodecTests
    {
        [Test]
        public void PackAndUnpack_PreservesHeterogeneousTableSchemas()
        {
            var weapon = TableSheet(
                "WeaponItem",
                "weapon-item",
                new[] { "#NAME", "ID", "Name", "Damage" },
                new[] { "#TYPE", "ID", "string", "int" },
                new[] { "", "sword", "Sword", "10" });
            var currency = TableSheet(
                "CurrencyItem",
                "currency-item",
                new[] { "#NAME", "ID", "Name", "Amount", "Premium" },
                new[] { "#TYPE", "ID", "string", "int", "bool" },
                new[] { "", "gem", "Gem", "5", "true" });

            var grouped = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[] { weapon, currency });
            var unpacked = GroupedSheetCodec.Unpack(grouped);

            Assert.That(GroupedSheetCodec.IsGrouped(grouped), Is.True);
            Assert.That(unpacked.Select(sheet => sheet.ID), Is.EqualTo(new[] { "weapon-item", "currency-item" }));
            Assert.That(unpacked[0].Values, Is.EqualTo(weapon.Values));
            Assert.That(unpacked[1].Values, Is.EqualTo(currency.Values));
            Assert.That(grouped.Values.Last()[0], Is.EqualTo(SheetConfig.GROUP_END_MARKER));
        }

        [Test]
        public void CsvRoundTrip_PreservesGroupedBlocks()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ODDB-GroupedCsv-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(directory);
                var grouped = GroupedSheetCodec.Pack(
                    "ItemView",
                    "item-view",
                    new[]
                    {
                        TableSheet(
                            "WeaponItem",
                            "weapon-item",
                            new[] { "#NAME", "ID", "Damage" },
                            new[] { "#TYPE", "ID", "int" },
                            new[] { "", "sword", "10" })
                    });

                CSVUtility.ExportSingleSheetToCSV(directory, grouped);
                var path = Directory.GetFiles(directory, "*.csv").Single();

                Assert.That(CSVUtility.TryImportSingleSheet(path, out var imported), Is.True);
                Assert.That(GroupedSheetCodec.Unpack(imported).Single().ID, Is.EqualTo("weapon-item"));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void CsvExport_RenamedGroupKeepsIdBoundFileInsteadOfCreatingDuplicate()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ODDB-GroupedCsv-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(directory);
                var original = GroupedSheetCodec.Pack(
                    "OldItemView",
                    "item-view",
                    new[] { TableSheet("WeaponItem", "weapon-item") });
                CSVUtility.ExportSingleSheetToCSV(directory, original);
                var originalPath = Directory.GetFiles(directory, "*.csv").Single();

                var renamed = GroupedSheetCodec.Pack(
                    "RenamedItemView",
                    "item-view",
                    new[] { TableSheet("CurrencyItem", "currency-item") });
                CSVUtility.ExportSingleSheetToCSV(directory, renamed);

                var paths = Directory.GetFiles(directory, "*.csv");
                Assert.That(paths, Has.Length.EqualTo(1));
                Assert.That(paths[0], Is.EqualTo(originalPath));
                Assert.That(CSVUtility.TryImportSingleSheet(paths[0], out var imported), Is.True);
                Assert.That(GroupedSheetCodec.Unpack(imported).Single().ID, Is.EqualTo("currency-item"));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void Pack_GroupByRootViewCombinesDescendantsAndKeepsStandaloneTable()
        {
            var database = new ODDatabase();
            var root = database.Views.Create(new ODDBID("item-view"));
            root.Name = "ItemView";
            var nested = database.Views.Create(new ODDBID("equipment-view"));
            nested.Name = "EquipmentView";
            nested.ParentView = root;

            var weapon = (Table)database.Tables.Create(new ODDBID("weapon-item"));
            weapon.Name = "WeaponItem";
            weapon.ParentView = nested;
            var currency = (Table)database.Tables.Create(new ODDBID("currency-item"));
            currency.Name = "CurrencyItem";
            currency.ParentView = root;
            var standalone = (Table)database.Tables.Create(new ODDBID("settings"));
            standalone.Name = "Settings";

            var logical = new[]
            {
                TableSheet("WeaponItem", "weapon-item"),
                TableSheet("CurrencyItem", "currency-item"),
                TableSheet("Settings", "settings")
            };

            var physical = SheetLayoutPlanner.Pack(
                logical,
                database,
                SheetLayoutMode.GroupByRootView);

            Assert.That(physical, Has.Count.EqualTo(2));
            var grouped = physical.Single(GroupedSheetCodec.IsGrouped);
            Assert.That(grouped.ID, Is.EqualTo("item-view"));
            Assert.That(
                GroupedSheetCodec.Unpack(grouped).Select(sheet => sheet.ID),
                Is.EqualTo(new[] { "weapon-item", "currency-item" }));
            Assert.That(physical.Single(sheet => !GroupedSheetCodec.IsGrouped(sheet)).ID, Is.EqualTo("settings"));
        }

        [Test]
        public void SelectedTableExport_IncludesEverySiblingInPhysicalGroup()
        {
            var database = new ODDatabase();
            var root = database.Views.Create(new ODDBID("item-view"));
            var weapon = (Table)database.Tables.Create(new ODDBID("weapon-item"));
            weapon.ParentView = root;
            var currency = (Table)database.Tables.Create(new ODDBID("currency-item"));
            currency.ParentView = root;

            var selected = SheetLayoutPlanner.SelectLogicalSheetsForExport(
                database,
                new ODDBSheetConverter(database),
                ExportScope.SingleTable("weapon-item"),
                SheetLayoutMode.GroupByRootView);

            Assert.That(selected.Select(sheet => sheet.ID), Is.EquivalentTo(new[] { "weapon-item", "currency-item" }));
        }

        [Test]
        public void SelectedRootViewExport_IncludesAllDescendantTables()
        {
            var database = new ODDatabase();
            var root = database.Views.Create(new ODDBID("item-view"));
            var nested = database.Views.Create(new ODDBID("equipment-view"));
            nested.ParentView = root;
            var otherRoot = database.Views.Create(new ODDBID("quest-view"));

            var weapon = (Table)database.Tables.Create(new ODDBID("weapon-item"));
            weapon.ParentView = nested;
            var currency = (Table)database.Tables.Create(new ODDBID("currency-item"));
            currency.ParentView = root;
            var quest = (Table)database.Tables.Create(new ODDBID("quest-item"));
            quest.ParentView = otherRoot;

            var selected = SheetLayoutPlanner.SelectLogicalSheetsForExport(
                database,
                new ODDBSheetConverter(database),
                ExportScope.ViewSubtree("item-view"),
                SheetLayoutMode.GroupByRootView);

            Assert.That(
                selected.Select(sheet => sheet.ID),
                Is.EquivalentTo(new[] { "weapon-item", "currency-item" }));
        }

        [Test]
        public void SelectedRootViewExport_PerTableIncludesOnlyDescendantTables()
        {
            var database = new ODDatabase();
            var root = database.Views.Create(new ODDBID("item-view"));
            var nested = database.Views.Create(new ODDBID("equipment-view"));
            nested.ParentView = root;
            var weapon = (Table)database.Tables.Create(new ODDBID("weapon-item"));
            weapon.ParentView = nested;
            var currency = (Table)database.Tables.Create(new ODDBID("currency-item"));
            currency.ParentView = root;
            database.Tables.Create(new ODDBID("settings"));

            var selected = SheetLayoutPlanner.SelectLogicalSheetsForExport(
                database,
                new ODDBSheetConverter(database),
                ExportScope.ViewSubtree("item-view"),
                SheetLayoutMode.PerTable);

            Assert.That(
                selected.Select(sheet => sheet.ID),
                Is.EquivalentTo(new[] { "weapon-item", "currency-item" }));
        }

        [Test]
        public void EmptyViewScope_ThrowsClearError()
        {
            var database = new ODDatabase();
            database.Views.Create(new ODDBID("empty-view"));

            Assert.That(
                () => SheetLayoutPlanner.ResolveTargetTableIds(
                    database,
                    ExportScope.ViewSubtree("empty-view")),
                Throws.InvalidOperationException.With.Message.Contains("contains no descendant tables"));
        }

        [Test]
        public void Unpack_RejectsDuplicateTableIds()
        {
            var grouped = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[] { TableSheet("WeaponItem", "weapon-item") });
            var duplicate = new List<List<string>>
            {
                new List<string> { SheetConfig.TABLE_MARKER, "weapon-item", "Duplicate" },
                new List<string> { SheetConfig.ROW_NAME_MARKER, "ID" },
                new List<string> { SheetConfig.ROW_TYPE_MARKER, "ID" },
                new List<string> { SheetConfig.TABLE_END_MARKER, "weapon-item" }
            };
            grouped.Values.InsertRange(grouped.Values.Count - 1, duplicate);

            Assert.That(() => GroupedSheetCodec.Unpack(grouped), Throws.InvalidOperationException);
        }

        [Test]
        public void ImportFilter_PrefersActiveGroupedLayoutOverLegacyDuplicate()
        {
            var database = new ODDatabase();
            var root = database.Views.Create(new ODDBID("item-view"));
            var table = (Table)database.Tables.Create(new ODDBID("weapon-item"));
            table.ParentView = root;

            var legacy = TableSheet("Legacy Weapon", "weapon-item");
            var groupedTable = GroupedSheetCodec.Unpack(GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[] { TableSheet("Grouped Weapon", "weapon-item") })).Single();

            var groupedMode = SheetLayoutPlanner.FilterImportedSheets(
                new[] { legacy, groupedTable },
                database,
                SheetLayoutMode.GroupByRootView);
            var legacyMode = SheetLayoutPlanner.FilterImportedSheets(
                new[] { legacy, groupedTable },
                database,
                SheetLayoutMode.PerTable);

            Assert.That(groupedMode.Single().Name, Is.EqualTo("Grouped Weapon"));
            Assert.That(legacyMode.Single().Name, Is.EqualTo("Legacy Weapon"));
        }

        [Test]
        public void ImportValidator_RejectsDuplicateLogicalTableSources()
        {
            var database = new ODDatabase();
            database.Tables.Create(new ODDBID("weapon-item"));
            var first = TableSheet("Weapon A", "weapon-item");
            var second = TableSheet("Weapon B", "weapon-item");

            var report = SheetImportValidator.Validate(
                new[] { first, second },
                ExportScope.EntireDatabase,
                database);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Any(issue => issue.Message.Contains("more than one")), Is.True);
        }

        [Test]
        public void ManagedColumnCount_IgnoresUserRowsAfterGroupEnd()
        {
            var grouped = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[] { TableSheet("WeaponItem", "weapon-item") });
            grouped.Values.Add(new List<string>
            {
                SheetConfig.ROW_NAME_MARKER,
                "User",
                "notes",
                "outside",
                "managed",
                "columns"
            });

            Assert.That(GroupedSheetCodec.GetManagedColumnCount(grouped), Is.EqualTo(4));
        }

        [Test]
        public void SelectedImport_DoesNotParseMalformedUnrelatedGroup()
        {
            var database = new ODDatabase();
            var selectedRoot = database.Views.Create(new ODDBID("selected-root"));
            var selectedTable = (Table)database.Tables.Create(new ODDBID("selected-table"));
            selectedTable.ParentView = selectedRoot;

            var selectedGroup = GroupedSheetCodec.Pack(
                "Selected",
                "selected-root",
                new[] { TableSheet("SelectedTable", "selected-table") });
            var malformed = GroupedSheetCodec.Pack(
                "Other",
                "other-root",
                new[] { TableSheet("OtherTable", "other-table") });
            malformed.Values.RemoveAt(malformed.Values.Count - 1);

            var scoped = SheetLayoutPlanner.FilterPhysicalSheetsForImport(
                new[] { malformed, selectedGroup },
                database,
                ExportScope.SingleTable("selected-table"),
                SheetLayoutMode.GroupByRootView);
            var unpacked = GroupedSheetCodec.UnpackAll(scoped);

            Assert.That(unpacked.Select(sheet => sheet.ID), Is.EqualTo(new[] { "selected-table" }));
        }

        [Test]
        public void SelectedRootViewImport_LoadsOnlyItsPhysicalGroup()
        {
            var database = new ODDatabase();
            var selectedRoot = database.Views.Create(new ODDBID("selected-root"));
            var first = (Table)database.Tables.Create(new ODDBID("first-table"));
            first.ParentView = selectedRoot;
            var second = (Table)database.Tables.Create(new ODDBID("second-table"));
            second.ParentView = selectedRoot;

            var selectedGroup = GroupedSheetCodec.Pack(
                "Selected",
                "selected-root",
                new[]
                {
                    TableSheet("First", "first-table"),
                    TableSheet("Second", "second-table")
                });
            var malformed = GroupedSheetCodec.Pack(
                "Other",
                "other-root",
                new[] { TableSheet("Other", "other-table") });
            malformed.Values.RemoveAt(malformed.Values.Count - 1);

            var scoped = SheetLayoutPlanner.FilterPhysicalSheetsForImport(
                new[] { malformed, selectedGroup },
                database,
                ExportScope.ViewSubtree("selected-root"),
                SheetLayoutMode.GroupByRootView);
            var unpacked = GroupedSheetCodec.UnpackAll(scoped);

            Assert.That(
                unpacked.Select(sheet => sheet.ID),
                Is.EqualTo(new[] { "first-table", "second-table" }));
        }

        [Test]
        public void ViewScopeValidator_RequiresEveryDescendantTable()
        {
            var database = new ODDatabase();
            var root = database.Views.Create(new ODDBID("item-view"));
            var first = (Table)database.Tables.Create(new ODDBID("first-table"));
            first.ParentView = root;
            var second = (Table)database.Tables.Create(new ODDBID("second-table"));
            second.ParentView = root;

            var report = SheetImportValidator.Validate(
                new[] { TableSheet("First", "first-table") },
                ExportScope.ViewSubtree("item-view"),
                database);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(
                report.Issues.Any(issue => issue.Message.Contains("second-table")),
                Is.True);
        }

        [Test]
        public void GoogleViewTypeNote_UsesReadableNameAndRestoresStableId()
        {
            var database = new ODDatabase();
            var referenced = database.Views.Create(new ODDBID("item-data-id"));
            referenced.Name = "ItemData";
            var source = TableSheet(
                "Owner",
                "owner",
                new[] { SheetConfig.ROW_NAME_MARKER, "ID", "Item" },
                new[] { SheetConfig.ROW_TYPE_MARKER, "ID", "view - item-data-id" });

            var exported = GoogleSheetViewTypeNotes.PrepareForExport(
                new[] { source },
                database).Single();
            var address = new SheetCellAddress(1, 2);
            Assert.That(exported.Values[1][2], Is.EqualTo("View-ItemData"));
            Assert.That(exported.CellNotes[address], Is.EqualTo("ODDB View ID: item-data-id"));

            var snapshot = new GoogleSheetSnapshot
            {
                Values = exported.Values.Select(row => new List<string>(row)).ToList()
            };
            snapshot.CellNotes[address] = exported.CellNotes[address];
            referenced.Name = "RenamedItemData";
            GoogleSheetViewTypeNotes.RestoreForImport(snapshot, database);

            Assert.That(snapshot.Values[1][2], Is.EqualTo("view - item-data-id"));
        }

        private static SheetInfo TableSheet(string name, string id, params string[][] rows)
        {
            if (rows == null || rows.Length == 0)
            {
                rows = new[]
                {
                    new[] { SheetConfig.ROW_NAME_MARKER, "ID" },
                    new[] { SheetConfig.ROW_TYPE_MARKER, "ID" }
                };
            }

            return new SheetInfo(name, id)
            {
                Values = rows.Select(row => row.ToList()).ToList()
            };
        }
    }
}
