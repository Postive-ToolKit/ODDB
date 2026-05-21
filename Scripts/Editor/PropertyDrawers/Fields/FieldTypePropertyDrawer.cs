using TeamODD.ODDB.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(FieldType))]
    public class FieldTypePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var paramProperty = property.FindPropertyRelative(FieldType.PARAM_FIELD);
            var typeKeyProperty = property.FindPropertyRelative(FieldType.TYPE_KEY_FIELD);

            var currentTypeKey = typeKeyProperty?.stringValue ?? string.Empty;
            var param = paramProperty?.stringValue ?? string.Empty;

            var title = BuildTitle(currentTypeKey, param);

            var button = new Button();
            button.text = title;
            button.clicked += () =>
            {
                var dropdown = new FieldTypeDropDown(new AdvancedDropdownState());
                dropdown.Show(button.worldBound);
                dropdown.OnSelectionChanged += (newTypeKey, newParam, name) =>
                {
                    if (typeKeyProperty != null)
                        typeKeyProperty.stringValue = newTypeKey;
                    if (paramProperty != null)
                        paramProperty.stringValue = newParam;

                    property.serializedObject.ApplyModifiedProperties();
                    button.text = BuildTitle(newTypeKey, newParam);
                };
            };
            return button;
        }

        private static string BuildTitle(string typeKey, string param)
        {
            if (string.IsNullOrEmpty(typeKey))
                return "<unset>";
            return string.IsNullOrEmpty(param) ? typeKey : $"{typeKey} - {param}";
        }
    }
}
