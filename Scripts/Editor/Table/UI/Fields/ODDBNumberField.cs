using UnityEngine.UIElements;
using System;

namespace TeamODD.ODDB.Editors.UI.Fields
{
    public class ODDBNumberField : IODDBField
    {
        private readonly FloatField _floatField;
        private readonly IntegerField _intField;
        private readonly bool _isInteger;
        
        public VisualElement Root => _isInteger ? _intField : _floatField as VisualElement;

        public ODDBNumberField(bool isInteger = false)
        {
            _isInteger = isInteger;
            if (isInteger)
            {
                _intField = new IntegerField();
                _intField.style.flexGrow = 1;
            }
            else
            {
                _floatField = new FloatField();
                _floatField.style.flexGrow = 1;
            }
        }

        public void SetValue(object value)
        {
            if (_isInteger)
            {
                if (int.TryParse(value?.ToString() ?? "0", out int result))
                {
                    _intField.value = result;
                }
            }
            else
            {
                if (float.TryParse(value?.ToString() ?? "0", out float result))
                {
                    _floatField.value = result;
                }
            }
        }

        public object GetValue()
        {
            return _isInteger ? _intField.value : _floatField.value;
        }

        public void RegisterValueChangedCallback(Action<object> callback)
        {
            if (_isInteger)
            {
                _intField.RegisterValueChangedCallback(evt => callback?.Invoke(evt.newValue));
            }
            else
            {
                _floatField.RegisterValueChangedCallback(evt => callback?.Invoke(evt.newValue));
            }
        }
    }
} 