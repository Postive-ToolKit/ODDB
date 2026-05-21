using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    public class FieldTypeDropDownItem : AdvancedDropdownItem
    {
        public string TypeKey { get; }
        public string Param { get; }

        public FieldTypeDropDownItem(string typeKey, string param, string displayName)
            : base(displayName)
        {
            TypeKey = typeKey ?? string.Empty;
            Param = param ?? string.Empty;
        }
    }
}
