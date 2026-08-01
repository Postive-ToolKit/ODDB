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
    public class EnumCellDrawer : IODDBReusableCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("enum") ?? new EnumSerializer();

        private sealed class ReusableEnumField : EnumField
        {
            public Action<Enum> CommitValue { get; set; }
        }

        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            var element = CreateReusablePropertyGUI(typeKey, param, commit);
            BindPropertyGUI(element, cell, typeKey, param);
            return element;
        }

        public VisualElement CreateReusablePropertyGUI(string typeKey, string param, Action<string> commit)
        {
            var field = new ReusableEnumField();
            var defaultValue = GetValue(string.Empty, param);
            if (defaultValue != null)
                field.Init(defaultValue);
            field.CommitValue = value => commit(_serializer.Serialize(value, param));
            field.RegisterValueChangedCallback(evt => field.CommitValue?.Invoke(evt.newValue));
            return field;
        }

        public void BindPropertyGUI(VisualElement element, Cell cell, string typeKey, string param)
        {
            var field = (ReusableEnumField)element;
            var value = GetValue(cell.SerializedData, param);
            field.SetValueWithoutNotify(value);
            if (string.IsNullOrEmpty(cell.SerializedData) && value != null)
                field.CommitValue?.Invoke(value);
        }

        private static Enum GetValue(string serializedData, string param)
        {
            var value = _serializer.Deserialize(serializedData, param) as Enum;
            return value ?? ODDBEnumUtility.GetEnumValues(param)?.Values.FirstOrDefault();
        }
    }
}
