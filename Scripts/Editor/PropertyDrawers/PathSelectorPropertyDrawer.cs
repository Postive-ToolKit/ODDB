using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(PathSelectorAttribute))]
    public class PathSelectorPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (PathSelectorAttribute)attribute;

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;

            var textField = new TextField(property.displayName);
            textField.style.flexGrow = 3;
            textField.BindProperty(property);
            container.Add(textField);

            var button = new Button(() =>
            {
                var pathSelector = new ODDBPathUtility();
                var path = pathSelector.GetPath(attr.BasePath, attr.BasePath);

                if (string.IsNullOrEmpty(path) == false)
                {
                    property.stringValue = path;
                    property.serializedObject.ApplyModifiedProperties();
                }
            })
            {
                style = { flexGrow = 1f},
                text = "Browse"
            };
            container.Add(button);
            return container;
        }
    }
}