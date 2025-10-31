using TeamODD.ODDB.Runtime.Enums;
using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    public class FieldTypeDropDownItem : AdvancedDropdownItem
    {
        public ODDBDataType Type { get; }
        public string Param { get; }

        public FieldTypeDropDownItem(string name, ODDBDataType type, string param = null) : base(name)
        {
            Type = type;
            Param = param ?? string.Empty;
        }
    }
}