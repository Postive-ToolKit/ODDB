using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEditor;
using UnityEngine;
namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ODDBID))]
    public class ODDBIDPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var idProperty = property.FindPropertyRelative(ODDBID.ID_FIELD_NAME);
            var idString = idProperty.stringValue;
            // GUI 그리기
            EditorGUI.BeginProperty(position, label, property);
            
            // 버튼 필드 그리기
            if (GUI.Button(position, new GUIContent(idProperty.stringValue)))
            {
                GUIUtility.systemCopyBuffer = idString;
                // Optionally, show a message to the user
                Debug.Log($"ID '{idString}' copied to clipboard."); 
            }
            EditorGUI.EndProperty();
        }
    }
}