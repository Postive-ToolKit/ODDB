using System;
using System.Linq;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.PropertyDrawers.Views
{
    /// <summary>
    /// Dropdown for selecting View IDs.
    /// </summary>
    public class ViewIdDropDown : AdvancedDropdown
    {
        public const string NONE_OPTION = "None";
        private readonly string _viewId;
        private IODDBEditorUseCase _editorUseCase;
        public event Action<string, string> OnSelectionChanged;
        public ViewIdDropDown(AdvancedDropdownState state, string viewId) : base(state)
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            _viewId = viewId;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            if (_editorUseCase == null)
                return new AdvancedDropdownItem("No Database");
            
            if (string.IsNullOrEmpty(_viewId))
                return new AdvancedDropdownItem("No View Selected");

            var tableList = _editorUseCase.GetInheritedTables(_viewId).ToList();
            
            var root = new AdvancedDropdownItem("Entities");

            root.AddChild(new ViewIdDropDownItem(NONE_OPTION, string.Empty));
            foreach (var table in tableList)
            {
                var rows = table.Rows;
                if (rows.Count == 0)
                    continue;
                var tableItem = new AdvancedDropdownItem(table.Name);
                foreach (var row in rows)
                {
                    var itemName = row.GetName();
                    var item = new ViewIdDropDownItem(itemName, row.ID.ToString());
                    tableItem.AddChild(item);
                }
                root.AddChild(tableItem);
            }
            
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is not ViewIdDropDownItem viewItem)
                return;
            OnSelectionChanged?.Invoke(viewItem.name, viewItem.Id);
        }
    }
}