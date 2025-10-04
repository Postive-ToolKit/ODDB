using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enum;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for boolean fields in the ODDB system.
    /// </summary>
    [CellDrawer(ODDBDataType.Bool)]
    public class BoolCellDrawer : BoolSerializer, IODDBCellDrawer
    {
        public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = (bool)(Deserialize(serializedData) ?? false);
            // GUI 그리기
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.Toggle(position, label, value);

            if (EditorGUI.EndChangeCheck())
            {
                var newSerializedData = Serialize(newValue);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }
    }
}