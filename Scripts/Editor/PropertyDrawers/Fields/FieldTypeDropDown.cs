using System;
using System.Linq;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Types;
using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    public class FieldTypeDropDown : AdvancedDropdown
    {
        // (typeKey, param, displayName)
        public event Action<string, string, string> OnSelectionChanged;

        public FieldTypeDropDown(AdvancedDropdownState state) : base(state)
        {
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Field Type");

            var byFolder = TypeRegistry.All
                .GroupBy(t => string.IsNullOrEmpty(t.Folder) ? "Other" : t.Folder)
                .OrderBy(g => g.Key);

            foreach (var group in byFolder)
            {
                var folderItem = new AdvancedDropdownItem(group.Key);
                foreach (var rt in group.OrderBy(t => t.Key))
                {
                    var selector = rt.RequiresParam
                        ? UseSubSelectorAttributeExtensions.FindParamSelector(rt.Key)
                        : null;

                    if (selector != null)
                    {
                        var subItem = new AdvancedDropdownItem(rt.Key);
                        foreach (var opt in selector.GetOptions())
                            subItem.AddChild(new FieldTypeDropDownItem(rt.Key, opt.Key, opt.Value));
                        folderItem.AddChild(subItem);
                        continue;
                    }

                    folderItem.AddChild(new FieldTypeDropDownItem(rt.Key, string.Empty, rt.Key));
                }
                root.AddChild(folderItem);
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is not FieldTypeDropDownItem fieldItem)
                return;
            OnSelectionChanged?.Invoke(fieldItem.TypeKey, fieldItem.Param, fieldItem.name);
        }
    }
}
