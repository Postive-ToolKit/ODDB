using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Entities;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public sealed class BindClassSelectView : ToolbarMenu, IHasView
    {
        public event Action<Type> OnBindClassChanged;
        private const string BIND_CLASS_NOT_FOUND = "None";
        private const string TEXT_PREFIX = "Bind : ";
        private Type _baseType;
        private readonly Dictionary<string,Type> _bindableClasses = new();
        private IODDBEditorUseCase _editorUseCase;
        
        public BindClassSelectView()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            _editorUseCase.OnViewChanged += OnViewChanged;
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnViewChanged(string viewId)
        {
            SetView(viewId);
        }
        
        public void SetView(string viewKey)
        {
            Type parentBind = null;
            var view = _editorUseCase.GetViewByKey(viewKey);
            if (view == null) {
                text = TEXT_PREFIX + BIND_CLASS_NOT_FOUND;
                return;
            }
            if(view.ParentView != null && view.ParentView.BindType != null)
                parentBind = view.ParentView.BindType;
            
            _baseType = parentBind;
            if(_baseType == null) 
                _baseType = typeof(ODDBEntity);
            CreateDropDown();
            if (view.BindType != null)
                text = TEXT_PREFIX + view.BindType.Name;
            else
                text = TEXT_PREFIX + BIND_CLASS_NOT_FOUND;
        }

        private void CreateDropDown()
        {
            _bindableClasses.Clear();
            menu.ClearItems();
            var baseType = _baseType;
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => {
                    try {
                        return asm.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        return e.Types.Where(t => t != null);
                    }
                })
                .Where(t => t != null && t.IsSubclassOf(baseType) && !t.IsAbstract)
                .ToArray();

            // add "None" option if baseType is ODDBEntity
            if (_baseType == typeof(ODDBEntity))
                menu.AppendAction(BIND_CLASS_NOT_FOUND, _ => OnSelectedChange(BIND_CLASS_NOT_FOUND));
            
            
            if (!baseType.IsAbstract)
            {
                _bindableClasses.Add(baseType.Name, baseType);
                menu.AppendAction(baseType.Name, _ => OnSelectedChange(baseType.Name));
            }
                
            
            foreach (var type in allTypes)
            {
                _bindableClasses[type.Name] = type;
                menu.AppendAction(type.Name, _ => OnSelectedChange(type.Name));
            }
        }
        
        private void OnSelectedChange(string newValue)
        {
            OnBindClassChanged?.Invoke(_bindableClasses.GetValueOrDefault(newValue));
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (_editorUseCase != null) {
                _editorUseCase.OnViewChanged -= OnViewChanged;
                _editorUseCase = null;
            }
        }
    }
}