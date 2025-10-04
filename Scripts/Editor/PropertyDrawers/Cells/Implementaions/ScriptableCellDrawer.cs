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
    /// Cell drawer for scriptable object based serializers
    /// </summary>
    [CellDrawer(ODDBDataType.ScriptableObject)]
    public class ScriptableCellDrawer : ResourceScriptableSerializer, IODDBCellDrawer
    {
        public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = Deserialize(serializedData) as ScriptableObject;
            // GUI 그리기
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            var newValue = (ScriptableObject)EditorGUI.ObjectField(position, label, value, typeof(ScriptableObject), false);

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