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
        private readonly List<string> _targetTables = new List<string>();
        
        public ODDBIDDropDown(AdvancedDropdownState state, params string[] targetTables) : base(state)
        {
            _targetTables.AddRange(targetTables);
            minimumSize = new Vector2(0, 200);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("ODDB IDs");
            var targetIDs = _targetTables.Count == 0 ? _service.GetAllTableID().ToList() : _targetTables;
            foreach (var tableId in targetIDs)
            {
                var tableItem = new ODDBIDDropDownItem(_service.GetPureName(tableId), tableId);
                var ids = _service.GetTableEntities(tableId);
                foreach (var id in ids)
                {
                    var idItem = new ODDBIDDropDownItem(_service.GetPureName(id), id);
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