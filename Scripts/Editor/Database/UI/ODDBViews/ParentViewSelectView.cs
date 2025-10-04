using System;
using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.UI.ViewWindows;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{

    public sealed class ParentViewSelectView : Button, IHasView
    {
        public event Action<IView> OnParentViewChanged;
        private const string INHERIT_PREFIX = "Inherit : ";
        private IODDBEditorUseCase _editorUseCase;
        private IView _view;
        public ParentViewSelectView()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            clicked += () =>
            {
                var window = new ViewSelectorWindow.Builder()
                    .SetTitle("Select Parent View")
                    .SetOnConfirm(parentView =>
                    {
                        text = parentView != null ? INHERIT_PREFIX + parentView.Name : INHERIT_PREFIX + "None";
                        OnParentViewChanged?.Invoke(parentView);
                    })
                    .SetIgnoreViews(new[] { _view });
                if(_view!.ParentView != null)
                    window.SetCurrentView(_view.ParentView);
                window.Build();
            };
        }
        public void SetView(string viewKey)
        {
            var view = _editorUseCase.GetViewByKey(viewKey);
            if (view == null) {
                return;
            }
            _view = view;
            
            if (_view.ParentView != null) {
                text = INHERIT_PREFIX + _view.ParentView.Name;
            } else {
                text = INHERIT_PREFIX + "None";
            }
        }
    }
}