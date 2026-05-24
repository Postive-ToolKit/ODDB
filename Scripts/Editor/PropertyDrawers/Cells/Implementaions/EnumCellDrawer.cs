using System;
using System.Linq;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for Enum data type.
    /// </summary>
    [CellDrawer("enum")]
    public class EnumCellDrawer : IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("enum") ?? new EnumSerializer();
        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            var value = _serializer.Deserialize(cell.SerializedData, param) as Enum;
            if (value == null)
            {
                var types = ODDBEnumUtility.GetEnumValues(param);
                if (types != null)
                {
                    value = types.Values.FirstOrDefault();
                    commit(_serializer.Serialize(value, param));
                }
            }

            var field = new EnumField()
            {
                value = value
            };
            field.Init(value);
            field.RegisterValueChangedCallback(evt =>
            {
                commit(_serializer.Serialize(evt.newValue, param));
            });
            return field;
        }
    }
}
