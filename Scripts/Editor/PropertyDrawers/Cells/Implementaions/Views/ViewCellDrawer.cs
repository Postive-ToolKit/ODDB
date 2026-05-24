using System;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEditor.IMGUI.Controls;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers.Views
{
    /// <summary>
    /// Property drawer for ODDBCell with view-reference data type.
    /// </summary>
    [CellDrawer("view")]
    public class ViewCellDrawer : StringSerializer, IODDBCellDrawer
    {
        private const string NOT_FOUND_TEXT = "No Entity Selected";
        private static IODDBEditorUseCase _useCase;
        private static IDataSerializer _serializer;
        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            if (_serializer == null)
                _serializer = TypeRegistry.Get("view") ?? new ViewRefSerializer();
            if (_useCase == null)
                _useCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();

            if (_useCase == null)
                return new Label("ODDB Editor Use Case Not Found");

            var formalRowId = cell.SerializedData;
            var title = NOT_FOUND_TEXT;

            if (_useCase.TryGetRow(param, formalRowId, out Row row))
            {
                title = ViewIdDropDownItem.FormatDisplayName(RowDisplayName.For(row), row.ID.ToString());
            }
            else
            {
                commit(string.Empty);
            }
            var button = new Button();
            button.text = title;
            button.clicked += () =>
            {
                var dropdown = new ViewIdDropDown(new AdvancedDropdownState(), param);
                dropdown.Show(button.worldBound);
                dropdown.OnSelectionChanged += (rowName, rowId) =>
                {
                    var newSerialized = _serializer.Serialize(rowId, string.Empty);
                    commit(newSerialized);
                    button.text = rowName.Equals(ViewIdDropDown.NONE_OPTION) ? NOT_FOUND_TEXT : rowName;
                };
            };
            return button;
        }
    }
}
