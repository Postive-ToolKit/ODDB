using System;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
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
            var typeProperty = property.FindPropertyRelative(FieldType.TYPE_FIELD);
            var paramProperty = property.FindPropertyRelative(FieldType.PARAM_FIELD);
            var typeKeyProperty = property.FindPropertyRelative(FieldType.TYPE_KEY_FIELD);

            // Display: prefer existing typeKey, else fall back to legacy enum.
            var currentTypeKey = typeKeyProperty != null && !string.IsNullOrEmpty(typeKeyProperty.stringValue)
                ? typeKeyProperty.stringValue
                : ((ODDBDataType)typeProperty.enumValueFlag).ToWireKey();
            var param = paramProperty != null ? paramProperty.stringValue : string.Empty;

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

                    // Back-compat: also write the legacy enum so older readers keep working.
                    if (Enum.TryParse<ODDBDataType>(newTypeKey, true, out var parsedEnum))
                    {
                        typeProperty.enumValueFlag = (int)parsedEnum;
                        if (paramProperty != null)
                            paramProperty.stringValue = newParam;
                    }
                    else
                    {
                        // Unknown to legacy enum — bucket as Custom and store typeKey in Param too.
                        typeProperty.enumValueFlag = (int)ODDBDataType.Custom;
                        if (paramProperty != null)
                            paramProperty.stringValue = string.IsNullOrEmpty(newParam) ? newTypeKey : newParam;
                    }

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
