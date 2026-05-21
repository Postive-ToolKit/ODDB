using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEditor;
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
        public VisualElement CreatePropertyGUI(SerializedProperty property, string typeKey, string param)
        {
            if (_serializer == null)
                _serializer = TypeRegistry.Get("view") ?? new ViewRefSerializer();
            if (_useCase == null)
                _useCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();

            if (_useCase == null)
                return new Label("ODDB Editor Use Case Not Found");

            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var formalRowId = targetField.stringValue;
            var title = NOT_FOUND_TEXT;

            var useCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            if (useCase.TryGetRow(param, formalRowId, out Row row))
            {
                title = ViewIdDropDownItem.FormatDisplayName(row.GetName(), row.ID.ToString());
            }
            else
            {
                targetField.stringValue = string.Empty;
                property.serializedObject.ApplyModifiedProperties();
            }
            var button = new Button();
            button.text = title;
            button.clicked += () =>
            {
                var dropdown = new ViewIdDropDown(new AdvancedDropdownState(), param);
                dropdown.Show(button.worldBound);
                dropdown.OnSelectionChanged += (rowName, rowId) =>
                {
                    formalRowId = _serializer.Serialize(rowId, string.Empty);
                    targetField.stringValue = formalRowId;
                    property.serializedObject.ApplyModifiedProperties();
                    button.text = rowName.Equals(ViewIdDropDown.NONE_OPTION) ? NOT_FOUND_TEXT : rowName;
                };
            };
            return button;
        }
    }
}
