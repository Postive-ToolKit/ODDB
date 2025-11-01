using System;
using System.Collections.Generic;
using Plugins.ODDB.Scripts.Editor.Utils;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(FieldType))]
    public class FieldTypePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var typeProperty = property.FindPropertyRelative(nameof(FieldType.Type));
            var paramProperty = property.FindPropertyRelative(nameof(FieldType.Param));
            
            var currentEnum = (ODDBDataType)typeProperty.enumValueFlag;
            var param = paramProperty != null ? paramProperty.stringValue : string.Empty;

            var title = currentEnum.GetName(param);
            
            var button = new Button();
            button.text = title;
            button.clicked += () =>
            {
                var dropdown = new FieldTypeDropDown(new AdvancedDropdownState());
                dropdown.Show(button.worldBound);
                dropdown.OnSelectionChanged += (newEnum, newParam, name) =>
                {
                    typeProperty.enumValueFlag = (int)newEnum;
                    if (paramProperty != null)
                        paramProperty.stringValue = newParam;
                    property.serializedObject.ApplyModifiedProperties();
                    button.text = newEnum.ToString();
                    if (string.IsNullOrEmpty(name) == false && newEnum.ToString().Equals(name) == false)
                        button.text += $" - {name}";
                };
            };
            return button;
        }
    }
}