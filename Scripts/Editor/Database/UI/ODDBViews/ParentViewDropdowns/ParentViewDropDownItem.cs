using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.UI.ParentViewDropdowns
{
    /// <summary>
    /// Dropdown item representing a parent view with an associated ID.
    /// </summary>
    public class ParentViewDropDownItem : AdvancedDropdownItem
    {
        public string Id;
        public ParentViewDropDownItem(string name, string id) : base(name)
        {
            Id = id;
        }
    }
}