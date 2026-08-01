using System;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CellDrawer("string")]
    public class StringCellDrawer : IODDBReusableCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("string") ?? new StringSerializer();
        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            var element = CreateReusablePropertyGUI(typeKey, param, commit);
            BindPropertyGUI(element, cell, typeKey, param);
            return element;
        }

        public VisualElement CreateReusablePropertyGUI(string typeKey, string param, Action<string> commit)
        {
            var textField = new TextField
            {
                isDelayed = true
            };
            textField.RegisterValueChangedCallback(evt => commit(_serializer.Serialize(evt.newValue, param)));
            return textField;
        }

        public void BindPropertyGUI(VisualElement element, Cell cell, string typeKey, string param)
            => ((TextField)element).SetValueWithoutNotify(
                _serializer.Deserialize(cell.SerializedData, param) as string ?? string.Empty);
    }
}
