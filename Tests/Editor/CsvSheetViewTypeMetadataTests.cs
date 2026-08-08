using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Utils.Sheets;
using TeamODD.ODDB.Editors.Utils.Sheets.CSV;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class CsvSheetViewTypeMetadataTests
    {
        [Test]
        public void PrepareForExport_WritesDisplayNameAndStableViewIdRow()
        {
            var database = new ODDatabase();
            var view = database.Views.Create(new ODDBID("item-data-id"));
            view.Name = "ItemData";

            var result = CsvSheetViewTypeMetadata.PrepareForExport(
                Sheet("view - item-data-id"),
                database);

            Assert.That(result.Values[1][2], Is.EqualTo("View-ItemData"));
            Assert.That(result.Values[2][0], Is.EqualTo(SheetConfig.ROW_VIEW_ID_MARKER));
            Assert.That(result.Values[2][2], Is.EqualTo("item-data-id"));
        }

        [Test]
        public void RestoreForImport_UsesStableIdAfterViewRenameAndRemovesMetadataRow()
        {
            var database = new ODDatabase();
            var view = database.Views.Create(new ODDBID("item-data-id"));
            view.Name = "ItemData";
            var exported = CsvSheetViewTypeMetadata.PrepareForExport(
                Sheet("view - item-data-id"),
                database);
            view.Name = "RenamedItemData";

            CsvSheetViewTypeMetadata.RestoreForImport(exported, database);

            Assert.That(exported.Values[1][2], Is.EqualTo("view - item-data-id"));
            Assert.That(exported.Values.Any(row => row[0] == SheetConfig.ROW_VIEW_ID_MARKER), Is.False);
        }

        [Test]
        public void RestoreForImport_PrefersIntentionalDisplayedViewChange()
        {
            var database = new ODDatabase();
            var item = database.Views.Create(new ODDBID("item-data-id"));
            item.Name = "ItemData";
            var currency = database.Views.Create(new ODDBID("currency-data-id"));
            currency.Name = "CurrencyData";
            var exported = CsvSheetViewTypeMetadata.PrepareForExport(
                Sheet("view - item-data-id"),
                database);
            exported.Values[1][2] = "View-CurrencyData";

            CsvSheetViewTypeMetadata.RestoreForImport(exported, database);

            Assert.That(exported.Values[1][2], Is.EqualTo("view - currency-data-id"));
        }

        [Test]
        public void GroupedRoundTrip_HandlesEveryTypeBlock()
        {
            var database = new ODDatabase();
            var view = database.Views.Create(new ODDBID("item-data-id"));
            view.Name = "ItemData";
            var grouped = GroupedSheetCodec.Pack(
                "ItemView",
                "item-view",
                new[]
                {
                    Sheet("view - item-data-id", "weapon"),
                    Sheet("view - item-data-id", "currency")
                });

            var exported = CsvSheetViewTypeMetadata.PrepareForExport(grouped, database);
            Assert.That(exported.Values.Count(row => row[0] == SheetConfig.ROW_VIEW_ID_MARKER), Is.EqualTo(2));

            CsvSheetViewTypeMetadata.RestoreForImport(exported, database);
            Assert.That(exported.Values.Any(row => row[0] == SheetConfig.ROW_VIEW_ID_MARKER), Is.False);
            Assert.That(GroupedSheetCodec.Unpack(exported), Has.Count.EqualTo(2));
        }

        private static SheetInfo Sheet(string type, string id = "table")
        {
            return new SheetInfo("Items", id)
            {
                Values = new List<List<string>>
                {
                    new List<string> { "#NAME", "ID", "Target" },
                    new List<string> { "#TYPE", "ID", type },
                    new List<string> { string.Empty, "row", "target" }
                }
            };
        }
    }
}
