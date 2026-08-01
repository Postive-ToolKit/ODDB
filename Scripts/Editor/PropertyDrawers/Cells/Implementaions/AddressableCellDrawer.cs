#if ADDRESSABLE_EXIST
using System;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Editors.PropertyDrawers.Serializers;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CellDrawer("addressable")]
    public class AddressableCellDrawer : IODDBReusableCellDrawer
    {
        private static readonly IDataSerializer _serializer = new EditorAddressableSerializer();
        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            var element = CreateReusablePropertyGUI(typeKey, param, commit);
            BindPropertyGUI(element, cell, typeKey, param);
            return element;
        }

        public VisualElement CreateReusablePropertyGUI(string typeKey, string param, Action<string> commit)
        {
            var referenceDataType = ODDBReferenceDataType.Object;
            if (Enum.TryParse(param, out ODDBReferenceDataType parsedType))
                referenceDataType = parsedType;
            var targetType = referenceDataType.GetReferenceDataBindType();
            var objectField = new ObjectField
            {
                objectType = targetType,
                allowSceneObjects = false
            };
            objectField.RegisterValueChangedCallback(evt => commit(_serializer.Serialize(evt.newValue, param)));
            return objectField;
        }

        public void BindPropertyGUI(VisualElement element, Cell cell, string typeKey, string param)
            => ((ObjectField)element).SetValueWithoutNotify(
                _serializer.Deserialize(cell.SerializedData, param) as Object);
    }
}
#endif
