using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Entities;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public sealed class ODDBBindClassSelectView : DropdownField, IODDBHasView
    {
        public event Action<Type> OnBindClassChanged;
        private const string BIND_CLASS_NOT_FOUND = "None";
        private Type _baseType;
        private readonly Dictionary<string,Type> _bindableClasses = new();
        private IODDBEditorUseCase _editorUseCase;
        
        public ODDBBindClassSelectView()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            label = "Bind Class";
            labelElement.style.minWidth = 0;
            labelElement.style.alignSelf = Align.FlexStart;
            _editorUseCase.OnViewChanged += OnViewChanged;
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<ChangeEvent<string>>(OnDropDownValueChanged);
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
                value = BIND_CLASS_NOT_FOUND;
                return;
            }
            if(view.ParentView != null && view.ParentView.BindType != null)
                parentBind = view.ParentView.BindType;
            
            _baseType = parentBind;
            if(_baseType == null) 
                _baseType = typeof(ODDBEntity);
            CreateDropDown();
            if (view.BindType != null)
                value = view.BindType.Name;
            else
                value = BIND_CLASS_NOT_FOUND;
        }

        private void CreateDropDown()
        {
            _bindableClasses.Clear();
            choices.Clear();
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
                choices.Add(BIND_CLASS_NOT_FOUND);
            
            
            if (!baseType.IsAbstract)
            {
                _bindableClasses.Add(baseType.Name, baseType);
                choices.Add(baseType.Name);
            }
                
            
            foreach (var type in allTypes)
            {
                _bindableClasses[type.Name] = type;
                choices.Add(type.Name);
            }
        }
        private void OnDropDownValueChanged(ChangeEvent<string> evt)
        {
            if (_bindableClasses.TryGetValue(evt.newValue, out var type)) {
                value = type.Name;
                OnBindClassChanged?.Invoke(type);
            }
            else {
                value = BIND_CLASS_NOT_FOUND;
                OnBindClassChanged?.Invoke(null);
            }
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