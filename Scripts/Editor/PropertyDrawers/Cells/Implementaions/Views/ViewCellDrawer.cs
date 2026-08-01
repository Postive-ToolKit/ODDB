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
    public class ViewCellDrawer : StringSerializer, IODDBReusableCellDrawer
    {
        private const string NOT_FOUND_TEXT = "No Entity Selected";
        private static IODDBEditorUseCase _useCase;
        private static IDataSerializer _serializer;
        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            var element = CreateReusablePropertyGUI(typeKey, param, commit);
            BindPropertyGUI(element, cell, typeKey, param);
            return element;
        }

        public VisualElement CreateReusablePropertyGUI(string typeKey, string param, Action<string> commit)
        {
            if (_serializer == null)
                _serializer = TypeRegistry.Get("view") ?? new ViewRefSerializer();
            if (_useCase == null)
                _useCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();

            if (_useCase == null)
                return new Label("ODDB Editor Use Case Not Found");

            var button = new Button { text = NOT_FOUND_TEXT };
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

        public void BindPropertyGUI(VisualElement element, Cell cell, string typeKey, string param)
        {
            if (element is not Button button || _useCase == null)
                return;

            var title = NOT_FOUND_TEXT;
            if (_useCase.TryGetRow(param, cell.SerializedData, out Row row))
                title = ViewIdDropDownItem.FormatDisplayName(RowDisplayName.For(row), row.ID.ToString());

            // Stale references are displayed but never mutated while binding. This
            // keeps scrolling side-effect free and avoids refresh recursion.
            button.text = title;
        }
    }
}
