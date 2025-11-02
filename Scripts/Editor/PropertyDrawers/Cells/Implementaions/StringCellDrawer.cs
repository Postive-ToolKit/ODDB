using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CellDrawer(ODDBDataType.String)]
    public class StringCellDrawer : IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer = ODDBDataType.String.GetDataSerializer();
        public VisualElement CreatePropertyGUI(SerializedProperty property, ODDBDataType dataType, string param)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = _serializer.Deserialize(serializedData, param)  as string;

            var textField = new TextField()
            {
                value = value ?? string.Empty
            };

            textField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = _serializer.Serialize(evt.newValue, param) ;
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return textField;
        }
    }
}