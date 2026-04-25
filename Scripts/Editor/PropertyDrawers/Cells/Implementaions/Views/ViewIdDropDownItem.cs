using TeamODD.ODDB.Editors.Utils;
using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.PropertyDrawers.Views
{
    /// <summary>
    /// Dropdown item representing a view with an associated ID.
    /// </summary>
    public class ViewIdDropDownItem : AdvancedDropdownItem
    {
        public string Id { get; }
        public ViewIdDropDownItem(string name, string id) : base(name)
        {
            Id = id;
        }

        public static string FormatDisplayName(string name, string id)
        {
            return ODDBEditorDisplayUtility.FormatNameWithId(name, id);
        }
    }
}
