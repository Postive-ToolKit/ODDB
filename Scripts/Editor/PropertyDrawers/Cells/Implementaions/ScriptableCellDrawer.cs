using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enum;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Cell drawer for scriptable object based serializers
    /// </summary>
    [CellDrawer(ODDBDataType.ScriptableObject)]
    public class ScriptableCellDrawer : ResourceScriptableSerializer, IODDBCellDrawer
    {
        public VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = Deserialize(serializedData) as ScriptableObject;

            var objectField = new ObjectField(property.displayName)
            {
                objectType = typeof(ScriptableObject),
                allowSceneObjects = false,
                value = value
            };

            objectField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = Serialize(evt.newValue as ScriptableObject);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return objectField;
        }
    }
}