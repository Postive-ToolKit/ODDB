using TeamODD.ODDB.Runtime.Settings.Attributes;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ODDBPathSelectorAttribute))]
    public class ODDBPathSelectorPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ODDBPathSelectorAttribute attr = (ODDBPathSelectorAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);

            Rect textFieldRect = position;
            textFieldRect.width -= 60;

            Rect buttonRect = position;
            buttonRect.x += position.width - 60;
            buttonRect.width = 60;

            EditorGUI.PropertyField(textFieldRect, property, label);

            if (GUI.Button(buttonRect, "Browse"))
            {
                var pathSelector = new ODDBPathUtility();

                string path = pathSelector.GetPath(attr.BasePath, attr.BasePath);

                if (!string.IsNullOrEmpty(path))
                {
                    property.stringValue = path;
                }
            }

            EditorGUI.EndProperty();
        }
    }
}