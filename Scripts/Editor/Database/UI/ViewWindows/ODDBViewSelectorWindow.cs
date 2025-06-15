using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI.ViewWindows
{
    public class ODDBViewSelectorWindow: EditorWindow
    {
        public event Action<IODDBView> OnConfirm;
        public event Action OnCancel;
        private IODDBView _view;
        private ODDBViewSelectView _viewSelectView;
        private List<IODDBView> _ignoredViews = new List<IODDBView>();
        private IODDBEditorUseCase _editorUseCase;
        
        private void CreateGUI()
        {
            _viewSelectView = new ODDBViewSelectView();
            _viewSelectView.OnViewSelected += view => _view = view;
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            rootVisualElement.Add(_viewSelectView);
            var button = new Button();
            button.text = "Confirm";
            button.clicked += ConfirmInput;
            rootVisualElement.Add(button);
        }

        private void ConfirmInput()
        {
            OnConfirm?.Invoke(_view);
            Close();
        }
        
        private void SetCurrentView(IODDBView currentView)
        {
            _view = currentView;
            _viewSelectView.SetSelectedView(currentView);
        }
        
        private void IgnoreViews(IEnumerable<IODDBView> views)
        {
            _ignoredViews = new List<IODDBView>(views);
            var targetView = _editorUseCase
                .GetPureViews()
                .Where(view => !_ignoredViews.Contains(view))
                .ToList();
            _viewSelectView.SetDataSource(targetView);
        }

        public class Builder
        {
            private string _title;
            private IODDBView _currentView;
            private IEnumerable<IODDBView> _ignoredViews;
            private Action<IODDBView> _onConfirm;
            private Action _onCancel;

            public Builder SetTitle(string title)
            {
                _title = title;
                return this;
            }

            public Builder SetOnConfirm(Action<IODDBView> onConfirm)
            {
                _onConfirm = onConfirm;
                return this;
            }

            public Builder SetOnCancel(Action onCancel)
            {
                _onCancel = onCancel;
                return this;
            }

            public Builder SetCurrentView(IODDBView view)
            {
                _currentView = view;
                return this;
            }
            
            public Builder SetIgnoreViews(IEnumerable<IODDBView> views)
            {
                _ignoredViews = views;
                return this;
            }

            public ODDBViewSelectorWindow Build()
            {
                var wnd = GetWindow<ODDBViewSelectorWindow>();
                wnd.titleContent = new GUIContent(_title);
                wnd.minSize = new Vector2(300, 600);
                wnd.maxSize = new Vector2(300, 1000);
                wnd.OnConfirm += _onConfirm;
                wnd.OnCancel += _onCancel;
                wnd.SetCurrentView(_currentView);
                wnd.IgnoreViews(_ignoredViews ?? Enumerable.Empty<IODDBView>());
                wnd.Show();
                return wnd;
            }
        }
    }
}