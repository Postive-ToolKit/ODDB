using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Dropdown item that holds an ODDB ID.
    /// </summary>
    public class ODDBIDDropDownItem : AdvancedDropdownItem
    {
        public string ID { get; }

        public ODDBIDDropDownItem(string name, string id) : base(name)
        {
            ID = id;
        }
    }
}