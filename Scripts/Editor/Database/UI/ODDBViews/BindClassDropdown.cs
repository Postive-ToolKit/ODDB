using System;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Runtime.Entities;
using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.UI
{
    public class BindClassDropdownItem : AdvancedDropdownItem
    {
        public Type BindType { get; }
        public BindClassDropdownItem(string name, Type type) : base(name) => BindType = type;
    }

    public class BindClassDropdown : AdvancedDropdown
    {
        private readonly Type _baseType;
        private readonly bool _allowNone;
        private readonly Type _currentType;
        public event Action<Type> OnBindClassSelected;

        public BindClassDropdown(AdvancedDropdownState state, Type baseType, bool allowNone, Type currentType) : base(state)
        {
            _baseType = baseType;
            _allowNone = allowNone;
            _currentType = currentType;
            minimumSize = new UnityEngine.Vector2(200, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Bind Class");
            
            if (_allowNone)
                root.AddChild(new BindClassDropdownItem("None", null));

            if (_baseType != null && !_baseType.IsAbstract)
                root.AddChild(new BindClassDropdownItem(_baseType.Name, _baseType));

            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => {
                    try { return asm.GetTypes(); }
                    catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
                })
                .Where(t => t != null && t.IsSubclassOf(_baseType) && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToArray();

            foreach (var type in allTypes)
                root.AddChild(new BindClassDropdownItem(type.Name, type));

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is BindClassDropdownItem bindItem)
                OnBindClassSelected?.Invoke(bindItem.BindType);
        }
    }
}
