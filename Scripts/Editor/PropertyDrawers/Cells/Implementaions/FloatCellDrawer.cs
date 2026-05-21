using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for float data type.
    /// </summary>
    [CellDrawer("float")]
    public class FloatCellDrawer : IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("float") ?? new FloatSerializer();
        public VisualElement CreatePropertyGUI(SerializedProperty property, string typeKey, string param)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = _serializer.Deserialize(serializedData, param) is float floatValue ? floatValue : 0f;

            var floatField = new FloatField()
            {
                value = value
            };

            floatField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = _serializer.Serialize(evt.newValue, param);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return floatField;
        }
    }
}
