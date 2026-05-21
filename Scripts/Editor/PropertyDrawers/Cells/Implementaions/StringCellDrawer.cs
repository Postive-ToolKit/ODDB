using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEditor;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CellDrawer("string")]
    public class StringCellDrawer : IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("string") ?? new StringSerializer();
        public VisualElement CreatePropertyGUI(SerializedProperty property, string typeKey, string param)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = _serializer.Deserialize(serializedData, param) as string;

            var textField = new TextField()
            {
                value = value ?? string.Empty
            };

            textField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = _serializer.Serialize(evt.newValue, param);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return textField;
        }
    }
}
