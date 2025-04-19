using System;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDBViewInfoView : GroupBox, IODDBHasView
    {
        public event Action<string> OnViewNameChanged;
        private IODDBView _view;
        private readonly TextField _tableNameInput;
        private readonly TextField _tableKeyInput;
        public ODDBViewInfoView()
        {
            ColorUtility.TryParseHtmlString("#080808", out Color color);
            style.backgroundColor = color;
            style.flexShrink = 1;
            style.flexDirection = FlexDirection.Column;

            // add input field for table name
            _tableNameInput = new TextField(label: "Table Name");
            _tableNameInput.style.flexGrow = 1;
            _tableNameInput.style.flexShrink = 0;
            _tableNameInput.RegisterValueChangedCallback(OnTableNameChangedEvent);
            Add(_tableNameInput);

            // add input field for table key
            _tableKeyInput = new TextField(label: "Table Key");
            _tableKeyInput.style.flexGrow = 1;
            _tableKeyInput.style.flexShrink = 0;
            _tableKeyInput.SetEnabled(false);
            Add(_tableKeyInput);
        }
        
        public void SetView(IODDBView view)
        {
            _view = view;
            _view = view;
            if (_view == null) {
                SetEnabled(false);
                _tableNameInput.SetEnabled(false);
                _tableNameInput.value = string.Empty;
                _tableKeyInput.value = string.Empty;
                return;
            }
            SetEnabled(true);
            _tableNameInput.SetEnabled(true);
            _tableNameInput.value = _view.Name;
            _tableKeyInput.value = _view.Key;
        }

        private void OnTableNameChangedEvent(ChangeEvent<string> evt)
        {
            if (_view == null)
                return;
            if (evt.newValue == _view.Name)
                return;
            OnViewNameChanged?.Invoke(evt.newValue);
        }


    }
}