using System;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Editors.PropertyDrawers.Serializers;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CellDrawer(ODDBDataType.Addressable)]
    public class AddressableCellDrawer : IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer = new EditorAddressableSerializer();
        public VisualElement CreatePropertyGUI(SerializedProperty property, ODDBDataType dataType, string param)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var referenceDataType = ODDBReferenceDataType.Object;
            if (Enum.TryParse(param, out ODDBReferenceDataType parsedType))
                referenceDataType = parsedType;
            var targetType = referenceDataType.GetReferenceDataBindType();
            var serializedData = targetField.stringValue;
            var value = _serializer.Deserialize(serializedData, param) as Object;

            var objectField = new ObjectField()
            {
                objectType = targetType,
                allowSceneObjects = false,
                value = value
            };

            objectField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = _serializer.Serialize(evt.newValue, param) ;
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return objectField;
        }
    }
}