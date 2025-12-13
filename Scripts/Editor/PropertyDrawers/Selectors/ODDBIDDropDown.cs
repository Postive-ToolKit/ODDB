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
            foreach (var targetType in _targetTables)
            {
                var tableItem = new ODDBIDDropDownItem(targetType.Name, targetType.FullName);
                var ids = _service.GetTypeEntities(targetType);
                foreach (var id in ids)
                {
                    var itemName = $"{targetType.Name} - {id}";
                    var idItem = new ODDBIDDropDownItem(itemName, id);
                    tableItem.AddChild(idItem);
                }
                root.AddChild(tableItem);
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