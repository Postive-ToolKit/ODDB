using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Dropdown item representing an ODDBID.
    /// </summary>
    public class ODDBIDDropDown : AdvancedDropdown
    {
        public event Action<string, string> OnIDSelected;
        private readonly ODDBSelectorService _service = new();
        private readonly List<Type> _targetTables = new List<Type>();
        
        public ODDBIDDropDown(AdvancedDropdownState state, params Type[] targetEntities) : base(state)
        {
            _targetTables.AddRange(targetEntities);
            minimumSize = new Vector2(0, 200);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("ODDB IDs");
            var options = _service.GetOptions(_targetTables.ToArray());

            foreach (var group in options.GroupBy(option => option.EntityType).OrderBy(group => group.Key.Name))
            {
                var typeItem = new ODDBIDDropDownItem(group.Key.Name, group.Key.FullName);
                foreach (var option in group.OrderBy(option => option.DisplayName))
                {
                    var idItem = new ODDBIDDropDownItem(option.DisplayName, option.ID);
                    typeItem.AddChild(idItem);
                }

                root.AddChild(typeItem);
            }

            return root;
        }
        
        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is not ODDBIDDropDownItem idItem)
                return;
            OnIDSelected?.Invoke(idItem.name, idItem.ID);
        }
    }
}
