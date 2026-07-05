using NUnit.Framework;
using TeamODD.ODDB.Editors.Utils.Sheets;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBSheetConverterTypeImportTests
    {
        [Test]
        public void ApplySheetToTable_UpdatesFieldTypesFromTypeRow()
        {
            var database = new ODDatabase();
            var textView = database.Views.Create(new ODDBID("text-view"));
            textView.Name = "TextData";
            var table = (Table)database.Tables.Create(new ODDBID("item"));
            table.AddField(new Field("Name", new FieldType("string", string.Empty)));
            table.AddField(new Field("Sprite", new FieldType("string", "Sprite")));

            var sheet = new SheetInfo("ItemData", "item");
            sheet.Values.Add(new System.Collections.Generic.List<string> { "#NAME", "ID", "Name", "Sprite" });
            sheet.Values.Add(new System.Collections.Generic.List<string> { "#TYPE", "ID", "View/TextData", "Addressable/Sprite" });
            sheet.Values.Add(new System.Collections.Generic.List<string> { string.Empty, "row1", "text_item_coin", "dummy_box" });

            new ODDBSheetConverter(database).ApplySheetToTable(table, sheet);

            Assert.That(table.TotalFields[0].Type.TypeKey, Is.EqualTo("view"));
            Assert.That(table.TotalFields[0].Type.Param, Is.EqualTo("text-view"));
            Assert.That(table.TotalFields[1].Type.TypeKey, Is.EqualTo("addressable"));
            Assert.That(table.TotalFields[1].Type.Param, Is.EqualTo("Sprite"));
            Assert.That(table.GetRow("row1").GetData(1).SerializedData, Is.EqualTo("dummy_box"));
        }
    }
}
