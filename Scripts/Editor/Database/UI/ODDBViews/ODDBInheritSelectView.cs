using System;
using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{

    public sealed class ODDBInheritSelectView : DropdownField, IODDBHasView
    {
        private const string INHERIT_NOT_FOUND = "None";
        private readonly Dictionary<string,IODDBView> _inheritableViews = new();
        public event Action<IODDBView> OnParentViewChanged;
        private IODDBEditorUseCase _editorUseCase;
        public ODDBInheritSelectView()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            CreateDropDown();
            label = "Inherit View";
            labelElement.style.minWidth = 0;
            labelElement.style.alignSelf = Align.FlexStart;
        }
        
        private void CreateDropDown()
        {
            var pureViews = _editorUseCase.GetPureViews();

            choices.Add(INHERIT_NOT_FOUND);
            
            foreach (var view in pureViews)
            {
                var selection = view.Name + " - " + view.ID;
                _inheritableViews[selection] = view;
                choices.Add(selection);
            }
            value = INHERIT_NOT_FOUND;
            
            RegisterCallback<ChangeEvent<string>>(OnDropDownValueChanged);
        }
        public void SetView(string viewKey)
        {
            var view = _editorUseCase.GetViewByKey(viewKey);
            if (view == null) {
                SetEnabled(false);
                value = INHERIT_NOT_FOUND;
                return;
            }
            choices.Remove(view.Name + " - " + view.ID);
            var parentView = view.ParentView;
            if (parentView == null) {
                value = INHERIT_NOT_FOUND;
                return;
            }
            value = parentView.Name + " - " + parentView.ID;
        }
        private void OnDropDownValueChanged(ChangeEvent<string> evt)
        {
            if (_inheritableViews.TryGetValue(evt.newValue, out var view)) {
                OnParentViewChanged?.Invoke(view);
            }
            else {
                value = INHERIT_NOT_FOUND;
                OnParentViewChanged?.Invoke(null);
            }
        }



    }
}