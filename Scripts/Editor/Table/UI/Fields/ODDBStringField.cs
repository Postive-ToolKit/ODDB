using UnityEngine.UIElements;
using System;

namespace TeamODD.ODDB.Editors.UI.Fields
{
    public class ODDBStringField : IODDBField
    {
        private readonly TextField _textField;
        public VisualElement Root => _textField;

        public ODDBStringField()
        {
            _textField = new TextField();
            _textField.style.flexGrow = 1;
        }

        public void SetValue(object value)
        {
            _textField.value = value?.ToString() ?? string.Empty;
        }

        public object GetValue()
        {
            return _textField.value;
        }

        public void RegisterValueChangedCallback(Action<object> callback)
        {
            _textField.RegisterValueChangedCallback(evt => callback?.Invoke(evt.newValue));
        }
    }
} 