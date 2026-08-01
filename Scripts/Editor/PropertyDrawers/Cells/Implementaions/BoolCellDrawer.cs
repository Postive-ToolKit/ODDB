using System;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for boolean fields in the ODDB system.
    /// </summary>
    [CellDrawer("bool")]
    public class BoolCellDrawer : IODDBReusableCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("bool") ?? new BoolSerializer();
        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            var element = CreateReusablePropertyGUI(typeKey, param, commit);
            BindPropertyGUI(element, cell, typeKey, param);
            return element;
        }

        public VisualElement CreateReusablePropertyGUI(string typeKey, string param, Action<string> commit)
        {
            var toggle = new Toggle
            {
                style = { alignSelf = Align.Center}
            };
            toggle.RegisterValueChangedCallback(evt => commit(_serializer.Serialize(evt.newValue, param)));
            return toggle;
        }

        public void BindPropertyGUI(VisualElement element, Cell cell, string typeKey, string param)
            => ((Toggle)element).SetValueWithoutNotify(
                (bool)(_serializer.Deserialize(cell.SerializedData, param) ?? false));
    }
}
