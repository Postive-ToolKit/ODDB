using UnityEngine.UIElements;
using System;

namespace TeamODD.ODDB.Editors.UI.Fields
{
    public class ODDBBoolField : IODDBField
    {
        private readonly VisualElement _container;
        private readonly Toggle _toggle;
        public VisualElement Root => _container;

        public ODDBBoolField()
        {
            // Create container
            _container = new VisualElement();
            _container.style.flexGrow = 0;
            _container.style.alignItems = Align.Center;
            _container.style.justifyContent = Justify.Center;

            // Create and setup toggle
            _toggle = new Toggle();
            _toggle.style.marginLeft = 0;
            _toggle.style.marginRight = 0;
            
            // Add toggle to container
            _container.Add(_toggle);
        }

        public void SetValue(object value)
        {
            if (bool.TryParse(value?.ToString() ?? "false", out bool result))
            {
                _toggle.value = result;
            }
        }

        public object GetValue()
        {
            return _toggle.value;
        }

        public void RegisterValueChangedCallback(Action<object> callback)
        {
            _toggle.RegisterValueChangedCallback(evt => callback?.Invoke(evt.newValue));
        }
    }
} 