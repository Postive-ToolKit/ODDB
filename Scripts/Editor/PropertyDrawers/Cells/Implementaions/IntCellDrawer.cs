using System;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for ODDBCell of type Int.
    /// </summary>
    [CellDrawer("int")]
    public class IntCellDrawer : IntSerializer, IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("int") ?? new IntSerializer();
        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            var value = (int)(_serializer.Deserialize(cell.SerializedData, param) ?? 0);

            var intField = new IntegerField()
            {
                value = value
            };

            intField.RegisterValueChangedCallback(evt =>
            {
                commit(_serializer.Serialize(evt.newValue, param));
            });

            return intField;
        }
    }
}
