using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Settings;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#else
using System.Text;
using UnityEngine.UIElements;
#endif

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
#if ODIN_INSPECTOR
    public class ODDBIDSelectorAttributeDrawer : OdinAttributeDrawer<ODDBIDSelectorAttribute, string>
    {
        private const float ButtonWidth = 80f;
        private const float Spacing = 2f;
        private const string ValidTooltip = "<color=green>Valid ID</color>";
        private const string InvalidTooltip = "<color=red>Invalid ID</color>";
        private const string ValidButtonText = "<color=green>✔</color> Search";
        private const string InvalidButtonText = "<color=red>✘</color> Search";
        
        private static GUIStyle _richButtonStyle;
        private readonly ODDBSelectorService _service = new ODDBSelectorService();
        
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (ODDBSettings.Setting.IsInitialized == false)
            {
                CallNextDrawer(label);
                return;
            }
            
            if (ODDBPort.IsInitialized == false)
                ODDBPort.Initialize();
            
            // Initialize rich text button style
            if (_richButtonStyle == null)
            {
                _richButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    richText = true
                };
            }
            
            var attr = Attribute;
            var stringValue = ValueEntry.SmartValue ?? string.Empty;
            var isValid = _service.IsValidID(stringValue);

            var fullRect = EditorGUILayout.GetControlRect();
            
            var labelRect = new Rect(fullRect.x, fullRect.y, EditorGUIUtility.labelWidth, fullRect.height);
            var buttonRect = new Rect(fullRect.xMax - ButtonWidth, fullRect.y, ButtonWidth, fullRect.height);
            var textFieldRect = new Rect(labelRect.xMax + Spacing, fullRect.y, 
                fullRect.xMax - labelRect.xMax - ButtonWidth - Spacing * 2, fullRect.height);
            
            if (label != null)
            {
                label.tooltip = GetTooltipText(isValid);
                EditorGUI.LabelField(labelRect, label);
            }
            
            var newValue = EditorGUI.TextField(textFieldRect, 
                new GUIContent(string.Empty, GetTooltipText(isValid)), stringValue);
            if (newValue != stringValue)
            {
                ValueEntry.SmartValue = newValue;
            }
            
            if (GUI.Button(buttonRect, new GUIContent(GetButtonText(isValid)), _richButtonStyle))
            {
                var dropdown = new ODDBIDDropDown(new AdvancedDropdownState(), attr.AllowEntities);
                dropdown.Show(fullRect);
                dropdown.OnIDSelected += (_, newID) => ValueEntry.SmartValue = newID;
            }
        }
        
        private string GetTooltipText(bool isValid) => isValid ? ValidTooltip : InvalidTooltip;
        private string GetButtonText(bool isValid) => isValid ? ValidButtonText : InvalidButtonText;
    }
#else

    [CustomPropertyDrawer(typeof(ODDBIDSelectorAttribute))]
    public class ODDBIDSelectorPropertyDrawer : PropertyDrawer
    {
        private const string ValidTooltip = "<color=green>Valid ID</color>";
        private const string InvalidTooltip = "<color=red>Invalid ID</color>";
        private const string ValidButtonText = "<color=green>✔</color>Search";
        private const string InvalidButtonText = "<color=red>✘</color>Search";
        
        private readonly ODDBSelectorService _service = new ODDBSelectorService();
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (ODDBSettings.Setting.IsInitialized == false)
                return base.CreatePropertyGUI(property);
            
            if (property.propertyType != SerializedPropertyType.String)
                return base.CreatePropertyGUI(property);
            
            if (ODDBPort.IsInitialized == false)
                ODDBPort.Initialize();
            
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
            container.Add(textArea);
            
            var button = new Button();
            button.text = GetButtonText(isValid);
            button.style.flexGrow = 2;
            button.clicked += () =>
            {
                var dropdown = new ODDBIDDropDown(new AdvancedDropdownState(), attr.AllowEntities);
                var rect = container.worldBound;
                dropdown.Show(rect);
                dropdown.OnIDSelected += (_, newID) =>
                {
                    property.stringValue = newID;
                    property.serializedObject.ApplyModifiedProperties();
                    button.text = GetButtonText(true);
                    container.tooltip = GetTooltipText(true);
                    textArea.value = newID;

                };
            };
            container.Add(button);
            
            textArea.RegisterValueChangedCallback(evt =>
            {
                property.stringValue = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
                var valid = _service.IsValidID(evt.newValue);
                button.text = GetButtonText(valid);
                container.tooltip = GetTooltipText(valid);
            });
            
            return container;
        }
        
        private string GetTooltipText(bool isValid) => isValid ? ValidTooltip : InvalidTooltip;
        private string GetButtonText(bool isValid) => isValid ? ValidButtonText : InvalidButtonText;
    }
#endif
}