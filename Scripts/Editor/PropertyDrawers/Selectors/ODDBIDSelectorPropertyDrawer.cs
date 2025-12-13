using System.Text;
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
            var isValid = _service.IsValidID(stringValue);
            
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.tooltip = GetTooltipText(isValid);
            
            var label = new Label(property.displayName);
            label.style.width = StyleKeyword.Auto;
            label.style.minWidth = 10;
            label.style.flexGrow = 2;
            container.Add(label);
            
            var textArea = new TextField();
            textArea.style.flexGrow = 6;
            textArea.value = stringValue;
            
            var button = new Button();
            button.text = GetButtonText(isValid);
            button.style.flexGrow = 2;
            button.clicked += () =>
            {
                var dropdown = new ODDBIDDropDown(new AdvancedDropdownState(), attr.AllowTables);
                
                var rect = container.worldBound;
                dropdown.Show(rect);
                dropdown.OnIDSelected += (newName, newID) =>
                {
                    property.stringValue = newID;
                    property.serializedObject.ApplyModifiedProperties();
                    button.text = GetButtonText(true);
                    container.tooltip = GetTooltipText(true);
                    textArea.value = newID;

                };
            };
            container.Add(button);
            
            textArea .RegisterValueChangedCallback(evt =>
            {
                property.stringValue = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
                var newID = evt.newValue;
                textArea.value = newID;
                var valid = _service.IsValidID(newID);
                button.text = GetButtonText(valid);
                container.tooltip = GetTooltipText(valid);
            });
            
            return container;
        }
        
        private string GetTooltipText(bool isValid)
        {
            return isValid ? "<color=green>Valid ID</color>" : "<color=red>Invalid ID</color>";
        }
        
        private string GetButtonText(bool isValid)
        {
            var sb = new StringBuilder();
            // Append X icon for invalid, check icon for valid
            if (isValid)
                sb.Append("<color=green>✔</color>");
            else
                sb.Append("<color=red>✘</color>");
            sb.Append("Search");
            return sb.ToString();
        }
    }
}