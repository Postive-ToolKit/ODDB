using System;
using System.Linq;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.UI.ParentViewDropdowns
{
    public class ParentViewDropDown : AdvancedDropdown
    {
        private string[] IgnoreIds { get; }
        private readonly IODDBEditorUseCase _editorUseCase;
        public event Action<string> OnParentViewSelected;
        
        public ParentViewDropDown(AdvancedDropdownState state, params string[] ignoredIds) : base(state)
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            IgnoreIds = ignoredIds;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            if (_editorUseCase == null)
                return new AdvancedDropdownItem("No Database");
            var ignoredIds = IgnoreIds.ToList();
            var targetView = _editorUseCase
                .GetPureViews()
                .Where(view => 
                    ignoredIds.Contains(view.ID) == false && 
                    ignoredIds.TrueForAll(view.IsChildOf) == false)
                .ToList();
            var root = new AdvancedDropdownItem("Views");
            foreach (var view in targetView)
            {
                var item = new ParentViewDropDownItem(view.Name, view.ID);
                root.AddChild(item);
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is not ParentViewDropDownItem parentViewDropDownItem)
                return;
            OnParentViewSelected?.Invoke(parentViewDropDownItem.Id);
        }
    }
}