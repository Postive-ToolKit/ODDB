using System;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Entities;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public sealed class BindClassSelectView : ToolbarButton, IHasView
    {
        public event Action<Type> OnBindClassChanged;
        private const string BIND_CLASS_NOT_FOUND = "None";
        private const string TEXT_PREFIX = "Bind Class: ";
        private Type _baseType;
        private IODDBEditorUseCase _editorUseCase;
        private string _currentViewId;
        private Type _currentBindType;
        
        public BindClassSelectView()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            _editorUseCase.OnViewChanged += OnViewChanged;
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            clicked += OnClicked;
        }

        private void OnClicked()
        {
            if (_baseType == null) return;
            
            var dropdown = new BindClassDropdown(
                new AdvancedDropdownState(), 
                _baseType, 
                _baseType == typeof(ODDBEntity), 
                _currentBindType
            );
            dropdown.OnBindClassSelected += type =>
            {
                _currentBindType = type;
                text = TEXT_PREFIX + (type?.Name ?? BIND_CLASS_NOT_FOUND);
                OnBindClassChanged?.Invoke(type);
            };
            dropdown.Show(worldBound);
        }

        private void OnViewChanged(string viewId)
        {
            if (string.IsNullOrEmpty(_currentViewId))
                return;

            if (_currentViewId == viewId)
            {
                SetView(_currentViewId);
                return;
            }

            var currentView = _editorUseCase?.GetViewByKey(_currentViewId);
            if (currentView?.ParentView != null && currentView.ParentView.ID == viewId)
                SetView(_currentViewId);
        }
        
        public void SetView(string viewKey)
        {
            _currentViewId = viewKey;
            Type parentBind = null;
            var view = _editorUseCase.GetViewByKey(viewKey);
            if (view == null) {
                text = TEXT_PREFIX + BIND_CLASS_NOT_FOUND;
                return;
            }
            if(view.ParentView != null && view.ParentView.BindType != null)
                parentBind = view.ParentView.BindType;
            
            _baseType = parentBind ?? typeof(ODDBEntity);
            _currentBindType = view.BindType;
            
            text = TEXT_PREFIX + (view.BindType?.Name ?? BIND_CLASS_NOT_FOUND);
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (_editorUseCase != null) {
                _editorUseCase.OnViewChanged -= OnViewChanged;
                _editorUseCase = null;
            }
            _currentViewId = null;
        }
    }
}
