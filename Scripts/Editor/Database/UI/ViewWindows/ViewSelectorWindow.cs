using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI.ViewWindows
{
    public class ViewSelectorWindow: EditorWindow
    {
        public event Action<IView> OnConfirm;
        public event Action OnCancel;
        private IView _view;
        private ViewSelectView _viewSelectView;
        private List<IView> _ignoredViews = new List<IView>();
        private IODDBEditorUseCase _editorUseCase;
        
        private void CreateGUI()
        {
            _viewSelectView = new ViewSelectView();
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
        
        private void SetCurrentView(IView currentView)
        {
            _view = currentView;
            _viewSelectView.SetSelectedView(currentView);
        }
        
        private void IgnoreViews(IEnumerable<IView> views)
        {
            _ignoredViews = new List<IView>(views);
            var targetView = _editorUseCase
                .GetPureViews()
                .Where(view => !_ignoredViews.Contains(view))
                .ToList();
            _viewSelectView.SetDataSource(targetView);
        }

        public class Builder
        {
            private string _title;
            private IView _currentView;
            private IEnumerable<IView> _ignoredViews;
            private Action<IView> _onConfirm;
            private Action _onCancel;

            public Builder SetTitle(string title)
            {
                _title = title;
                return this;
            }

            public Builder SetOnConfirm(Action<IView> onConfirm)
            {
                _onConfirm = onConfirm;
                return this;
            }

            public Builder SetOnCancel(Action onCancel)
            {
                _onCancel = onCancel;
                return this;
            }

            public Builder SetCurrentView(IView view)
            {
                _currentView = view;
                return this;
            }
            
            public Builder SetIgnoreViews(IEnumerable<IView> views)
            {
                _ignoredViews = views;
                return this;
            }

            public ViewSelectorWindow Build()
            {
                var wnd = GetWindow<ViewSelectorWindow>();
                wnd.titleContent = new GUIContent(_title);
                wnd.minSize = new Vector2(300, 600);
                wnd.maxSize = new Vector2(300, 1000);
                wnd.OnConfirm += _onConfirm;
                wnd.OnCancel += _onCancel;
                wnd.SetCurrentView(_currentView);
                wnd.IgnoreViews(_ignoredViews ?? Enumerable.Empty<IView>());
                wnd.Show();
                return wnd;
            }
        }
    }
}