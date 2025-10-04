using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime.Attributes;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(PathSelectorAttribute))]
    public class PathSelectorPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PathSelectorAttribute attr = (PathSelectorAttribute)attribute;

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