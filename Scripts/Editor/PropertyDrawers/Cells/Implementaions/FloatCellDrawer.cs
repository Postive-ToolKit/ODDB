using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enum;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for float data type.
    /// </summary>
    [CellDrawer(ODDBDataType.Float)]
    public class FloatCellDrawer : FloatSerializer, IODDBCellDrawer
    {
        public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = Deserialize(serializedData) is float floatValue ? floatValue : 0f;

            // Draw the property in the inspector
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.FloatField(position, label, value);

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