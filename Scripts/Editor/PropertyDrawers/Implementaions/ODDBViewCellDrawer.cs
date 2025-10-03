using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Enum;
using TeamODD.ODDB.Runtime.Data.Serializers;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for ODDBCell with string data type
    /// </summary>
    [ODDBCellDrawer(ODDBDataType.View)]
    public class ODDBViewCellDrawer : ODDBStringSerializer, IODDBCellDrawer
    {
        public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetField = property.FindPropertyRelative(ODDBCell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = Deserialize(serializedData) as string;
            // GUI 그리기
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            
            
            // 버튼 만들고 클릭시 일단은 로그 찍기
            if (GUI.Button(position, string.IsNullOrEmpty(value) ? "No Entity Assigned" : value))
            {
                Debug.Log($"View Button Clicked! Current Value: {value}");
            }


            EditorGUI.EndProperty();
        }
    }
}