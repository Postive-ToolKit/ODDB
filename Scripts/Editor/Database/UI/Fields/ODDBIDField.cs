using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI.Fields
{
    public class ODDBIDField : IODDBField
    {
        private readonly Button _keyButton;
        public VisualElement Root => _keyButton;

        private string _key;

        public ODDBIDField()
        {
            // Create button for key field
            _keyButton = new Button();
            _keyButton.text = "No Key";
            _keyButton.style.flexGrow = 1;
            _keyButton.style.alignItems = Align.Center;
            _keyButton.style.justifyContent = Justify.Center;
            _keyButton.style.marginBottom = 0f;
            _keyButton.style.marginTop = 0f;
            _keyButton.style.marginLeft = 0f;
            _keyButton.style.marginRight = 0f;
            _keyButton.clicked += () =>
            {
                // copy key to clipboard
                GUIUtility.systemCopyBuffer = _key;
                // Optionally, show a message to the user
                Debug.Log($"Key '{_key}' copied to clipboard.");
            };
        }

        public void SetValue(object value)
        {
            _key = value?.ToString() ?? "No Key";
            _keyButton.text = _key;
        }

        public object GetValue()
        {
            return _key;
        }

        public void RegisterValueChangedCallback(Action<object> callback)
        {
            _keyButton.RegisterCallback<ClickEvent>(evt => callback?.Invoke(_key));
        }
    }
}