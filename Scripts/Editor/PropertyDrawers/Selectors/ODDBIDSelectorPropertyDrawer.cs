using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Settings;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ODDBIDSelectorAttribute))]
    public class ODDBIDSelectorPropertyDrawer : PropertyDrawer
    {
        private static readonly ODDBSelectorService _service = new ODDBSelectorService();
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (ODDBSettings.Setting.IsInitialized == false)
                return base.CreatePropertyGUI(property);
            
            if (property.propertyType != SerializedPropertyType.String)
                return base.CreatePropertyGUI(property);
            
            var attr = (ODDBIDSelectorAttribute)attribute;
            
            var stringValue = property.stringValue;
            if (_service.IsValidID(stringValue) == false)
            {
                stringValue = string.Empty;
                property.stringValue = stringValue;
                property.serializedObject.ApplyModifiedProperties();
            }
            
            var title = _service.GetName(stringValue);
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.tooltip = property.stringValue;
            
            var label = new Label(property.displayName);
            label.style.width = StyleKeyword.Auto;
            label.style.minWidth = 10;
            label.style.flexGrow = 4;
            container.Add(label);
            
            var button = new Button();
            button.text = title;
            button.style.flexGrow = 6;
            button.clicked += () =>
            {
                var dropdown = new ODDBIDDropDown(new AdvancedDropdownState(), attr.AllowTables);
                
                var rect = container.worldBound;
                dropdown.Show(rect);
                dropdown.OnIDSelected += (newName, newID) =>
                {
                    property.stringValue = newID;
                    property.serializedObject.ApplyModifiedProperties();
                    button.text = _service.GetName(newID);
                    container.tooltip = newID;
                };
            };
            container.Add(button);
            return container;
            
        }
    }
}