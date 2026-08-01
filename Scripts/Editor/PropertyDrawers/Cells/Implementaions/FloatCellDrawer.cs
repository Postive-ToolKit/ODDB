using System;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for float data type.
    /// </summary>
    [CellDrawer("float")]
    public class FloatCellDrawer : IODDBReusableCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("float") ?? new FloatSerializer();
        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            var element = CreateReusablePropertyGUI(typeKey, param, commit);
            BindPropertyGUI(element, cell, typeKey, param);
            return element;
        }

        public VisualElement CreateReusablePropertyGUI(string typeKey, string param, Action<string> commit)
        {
            var floatField = new FloatField
            {
                isDelayed = true
            };
            floatField.RegisterValueChangedCallback(evt => commit(_serializer.Serialize(evt.newValue, param)));
            return floatField;
        }

        public void BindPropertyGUI(VisualElement element, Cell cell, string typeKey, string param)
            => ((FloatField)element).SetValueWithoutNotify(
                _serializer.Deserialize(cell.SerializedData, param) is float value ? value : 0f);
    }
}
