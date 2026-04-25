using System;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.UI.ParentViewDropdowns;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;

namespace TeamODD.ODDB.Editors.UI
{

    public sealed class ParentViewSelectView : ToolbarButton, IHasView
    {
        public event Action<IView> OnParentViewChanged;
        private const string INHERIT_PREFIX = "Inherits: ";
        private IODDBEditorUseCase _editorUseCase;
        private IView _view;
        public ParentViewSelectView()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            clicked += () =>
            {
                var parentViewDropDown = new ParentViewDropDown(new AdvancedDropdownState(), _view?.ID ?? string.Empty);
                parentViewDropDown.Show(worldBound);
                parentViewDropDown.OnParentViewSelected += (viewId) =>
                {
                    var resultView = _editorUseCase.GetViewByKey(viewId);
                    text = INHERIT_PREFIX + FormatViewReference(resultView);
                    OnParentViewChanged?.Invoke(resultView);
                };
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
                text = INHERIT_PREFIX + FormatViewReference(_view.ParentView);
            } else {
                text = INHERIT_PREFIX + "None";
            }
        }

        private static string FormatViewReference(IView view)
        {
            if (view == null)
                return "None";

            var id = view.ID?.ToString();
            return ODDBEditorDisplayUtility.FormatNameWithId(view.Name, id);
        }
    }
}
