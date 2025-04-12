using UnityEngine.UIElements;
using System;

namespace TeamODD.ODDB.Editors.UI.Fields
{
    public class ODDBBoolField : IODDBField
    {
        private readonly Toggle _toggle;
        public VisualElement Root => _toggle;

        public ODDBBoolField()
        {
            _toggle = new Toggle();
            _toggle.style.flexGrow = 1;
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