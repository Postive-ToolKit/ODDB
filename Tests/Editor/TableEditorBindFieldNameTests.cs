using System;
using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors;
using TeamODD.ODDB.Editors.UI;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBHeaderViewTests
    {
        [Test]
        public void HeaderFields_NameCommitsDelayedAndIdIsReadOnly()
        {
            var database = new ODDatabase();
            var view = database.Views.Create(new ODDBID("header-test"));
            view.Name = "Header Test";
            var header = new ODDBHeaderView(null);

            header.UpdateView(view, ODDBViewType.View);

            var nameField = header.Q<TextField>("oddb-view-name");
            var idField = header.Q<TextField>("oddb-view-id");
            Assert.That(nameField, Is.Not.Null);
            Assert.That(nameField.isDelayed, Is.True);
            Assert.That(idField, Is.Not.Null);
            Assert.That(idField.isReadOnly, Is.True);
            Assert.That(idField.value, Is.EqualTo("header-test"));
        }
    }

    public sealed class TableEditorBindFieldNameTests
    {
        [Test]
        public void GetBindTypeFieldName_SkipsODDBEntityInfrastructureFields()
        {
            Assert.That(TableEditor.GetBindTypeFieldName(typeof(BoundItemData), 0), Is.EqualTo("Name"));
            Assert.That(TableEditor.GetBindTypeFieldName(typeof(BoundItemData), 1), Is.EqualTo("Desc"));
        }

        [Test]
        public void CellColumn_ReusesControlAndHidesItForInvalidBinding()
        {
            var database = (ODDatabase)ODDBEditorRuntime.UseCase.DataBase;
            var tableId = new ODDBID($"codex-reuse-{Guid.NewGuid():N}");
            var table = (Table)database.Tables.Create(tableId);
            try
            {
                table.AddField(new Field("Name", new FieldType("string", string.Empty)));
                table.AddRow(new ODDBID("first"));
                table.AddRow(new ODDBID("second"));
                table.GetRow("first").SetData(0, "one", true);
                table.GetRow("second").SetData(0, "two", true);

                var editor = new TableEditor();
                editor.SetView(tableId.ToString());
                var column = editor.columns.ElementAt(1);
                var host = column.makeCell();

                column.bindCell(host, 0);
                var field = host.Q<TextField>();
                Assert.That(field, Is.Not.Null);
                Assert.That(field.value, Is.EqualTo("one"));

                column.bindCell(host, 1);
                Assert.That(host.Q<TextField>(), Is.SameAs(field));
                Assert.That(field.value, Is.EqualTo("two"));
                Assert.That(field.style.display.value, Is.EqualTo(DisplayStyle.Flex));

                column.bindCell(host, int.MaxValue);
                Assert.That(field.style.display.value, Is.EqualTo(DisplayStyle.None));

                editor.SetView("missing-table");
                Assert.That(editor.itemsSource, Is.Null);
                Assert.That(editor.columns, Is.Empty);
            }
            finally
            {
                database.Tables.Delete(tableId);
            }
        }

        private sealed class BoundItemData : ODDBEntity
        {
#pragma warning disable 0169
            private string _name;
            private string _desc;
#pragma warning restore 0169
        }
    }

    public sealed class ODDBEditorWindowDelayedFieldTests
    {
        [Test]
        public void FindDelayedField_ReturnsSupportedDelayedAncestorOfFocusedInput()
        {
            var fields = new VisualElement[]
            {
                new TextField { isDelayed = true },
                new IntegerField { isDelayed = true },
                new FloatField { isDelayed = true }
            };

            foreach (var field in fields)
            {
                var focusedInput = new VisualElement();
                field.Add(focusedInput);

                Assert.That(ODDBEditorWindow.FindDelayedField(focusedInput), Is.SameAs(field));
            }
        }

        [Test]
        public void FindDelayedField_IgnoresImmediateField()
        {
            var field = new TextField { isDelayed = false };
            var focusedInput = new VisualElement();
            field.Add(focusedInput);

            Assert.That(ODDBEditorWindow.FindDelayedField(focusedInput), Is.Null);
        }
    }
}
