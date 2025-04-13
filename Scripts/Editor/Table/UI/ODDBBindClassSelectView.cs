using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Runtime.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public sealed class ODDBBindClassSelectView : DropdownField
    {
        private const string BIND_CLASS_NOT_FOUND = "Bind class not found";
        private readonly Dictionary<string,Type> _bindableClasses = new();
        public event Action<Type> OnBindClassChanged;
        public ODDBBindClassSelectView()
        {
            CreateDropDown();
            label = "Bind Class";
            labelElement.style.minWidth = 0;
            labelElement.style.alignSelf = Align.FlexStart;
        }
        
        private void CreateDropDown()
        {
            var baseType = typeof(ODDBEntity);
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

            foreach (var type in allTypes)
            {
                _bindableClasses[type.Name] = type;
                choices.Add(type.Name);
            }
            value = BIND_CLASS_NOT_FOUND;
            
            RegisterCallback<ChangeEvent<string>>(OnDropDownValueChanged);
        }
        
        public void SetType(Type type)
        {
            if (type == null) {
                value = BIND_CLASS_NOT_FOUND;
                return;
            }
            var typeName = type.Name;
            value = typeName;
        }
        private void OnDropDownValueChanged(ChangeEvent<string> evt)
        {
            if (_bindableClasses.TryGetValue(evt.newValue, out var type)) {
                value = type.Name;
                OnBindClassChanged?.Invoke(type);
            }
            else {
                value = BIND_CLASS_NOT_FOUND;
            }
        }

        
    }
}