using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    public class FieldTypeDropDown : AdvancedDropdown
    {
        private static List<ODDBDataType> CachedEnums
        {
            get
            {
                if (_cachedEnums != null) 
                    return _cachedEnums;
                _cachedEnums = new List<ODDBDataType>((ODDBDataType[])Enum.GetValues(typeof(ODDBDataType)));
                return _cachedEnums;
            }
        }
        private static List<ODDBDataType> _cachedEnums;
        public event Action<ODDBDataType, string> OnSelectionChanged;
        
        public FieldTypeDropDown(AdvancedDropdownState state) : base(state)
        {
            
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("FieldTypes");
            
            var advSelections = new Dictionary<ODDBDataType,AdvancedDropdownItem>();
            
            foreach (var e in CachedEnums)
            {
                if (e.GetDataTypeOption().IsHideInSelector)
                    continue;
                advSelections.Add(e, new FieldTypeDropDownItem(e.ToString(), e));
                root.AddChild(advSelections[e]);
                
                var enumSelections = e.GetTypeSubSelector();
                if (enumSelections == null)
                    continue;
                foreach (var (realValue, selectionValue) in enumSelections.GetOptions())
                    advSelections[e].AddChild(new FieldTypeDropDownItem(selectionValue, e, realValue));
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is not FieldTypeDropDownItem fieldItem)
                return;
            OnSelectionChanged?.Invoke(fieldItem.Type, fieldItem.Param);
        }
    }
}