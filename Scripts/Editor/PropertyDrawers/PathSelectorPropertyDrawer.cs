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
        private const float ButtonWidth = 64f;
        private const float Spacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var contentRect = EditorGUI.PrefixLabel(position, label);
            var fieldRect = new Rect(
                contentRect.x,
                contentRect.y,
                Mathf.Max(0f, contentRect.width - ButtonWidth - Spacing),
                contentRect.height);
            var buttonRect = new Rect(
                fieldRect.xMax + Spacing,
                contentRect.y,
                ButtonWidth,
                contentRect.height);

            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.TextField(fieldRect, property.stringValue);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = value;

            if (GUI.Button(buttonRect, "Browse"))
                SelectPath(property, (PathSelectorAttribute)attribute);

            EditorGUI.EndProperty();
        }

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
                SelectPath(property, attr);
            })
            {
                style = { flexGrow = 1f},
                text = "Browse"
            };
            container.Add(button);
            return container;
        }

        private static void SelectPath(
            SerializedProperty property,
            PathSelectorAttribute attr)
        {
            var pathSelector = new ODDBPathUtility();
            var path = pathSelector.GetPath(attr.BasePath, attr.BasePath);
            if (string.IsNullOrEmpty(path))
                return;

            property.stringValue = path;
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
