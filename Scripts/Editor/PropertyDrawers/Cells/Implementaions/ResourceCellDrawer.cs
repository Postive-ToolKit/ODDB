using System;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CellDrawer("resource")]
    public class ResourceCellDrawer : IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("resource") ?? new ResourceSerializer();
        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            var referenceDataType = ODDBReferenceDataType.Object;
            if (Enum.TryParse(param, out ODDBReferenceDataType parsedType))
                referenceDataType = parsedType;
            var targetType = referenceDataType.GetReferenceDataBindType();
            var value = _serializer.Deserialize(cell.SerializedData, param) as Object;

            var objectField = new ObjectField()
            {
                objectType = targetType,
                allowSceneObjects = false,
                value = value
            };

            objectField.RegisterValueChangedCallback(evt =>
            {
                commit(_serializer.Serialize(evt.newValue, param));
            });

            return objectField;
        }
    }
}
