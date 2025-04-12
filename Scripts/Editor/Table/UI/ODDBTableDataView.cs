using System;
using TeamODD.ODDB.Editors.UI;
using TeamODD.ODDB.Scripts.Runtime.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    [UxmlElement]
    public partial class ODDBTableDataView : VisualElement
    {
        private readonly TextField _tableNameInput;
        private readonly TextField _tableKeyInput;
        private readonly GroupBox _toolBox;
        private readonly ODDBMultiColumnListView _multiColumnListView;

        public event Action<ODDBTable> OnTableNameChanged;
        
        private ODDBTable _table;
        public ODDBTableDataView()
        {
            style.flexGrow = 1;
            style.flexShrink = 0;
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Stretch; // FlexStart에서 Stretch로 변경


            var tableInfo = new GroupBox();
            ColorUtility.TryParseHtmlString("#080808", out Color color);
            tableInfo.style.backgroundColor = color;
            tableInfo.style.flexShrink = 1;
            tableInfo.style.flexDirection = FlexDirection.Column;

            // add input field for table name
            _tableNameInput = new TextField(label: "Table Name");
            _tableNameInput.style.flexGrow = 1;
            _tableNameInput.style.flexShrink = 0;
            _tableNameInput.RegisterValueChangedCallback(OnTableNameChangedEvent);
            tableInfo.Add(_tableNameInput);

            // add input field for table key
            _tableKeyInput = new TextField(label: "Table Key");
            _tableKeyInput.style.flexGrow = 1;
            _tableKeyInput.style.flexShrink = 0;
            _tableKeyInput.SetEnabled(false);
            tableInfo.Add(_tableKeyInput);

            Add(tableInfo);

            _toolBox = new GroupBox();
            _toolBox.style.flexShrink = 1;
            _toolBox.style.flexDirection = FlexDirection.Row;

            Add(_toolBox);

            _multiColumnListView = new ODDBMultiColumnListView();
            _multiColumnListView.style.flexGrow = 1;
            _multiColumnListView.style.flexShrink = 0;
            Add(_multiColumnListView);
        }
        public void SetTable(ODDBTable table)
        {
            _table = table;
            if (table == null) {
                _tableNameInput.SetEnabled(false);
                _tableNameInput.value = string.Empty;
                _tableKeyInput.value = string.Empty;
                return;
            }
            _tableNameInput.SetEnabled(true);
            _tableNameInput.value = _table.Name;
            _tableKeyInput.value = _table.Key;
            _multiColumnListView.SetTable(table);
        }
        private void OnTableNameChangedEvent(ChangeEvent<string> evt)
        {
            if (evt.newValue.Equals(_table.Name))
                return;
            if (_table == null)
                return;
            _table.Name = evt.newValue;
            OnTableNameChanged?.Invoke(_table);
        }
    }
}
