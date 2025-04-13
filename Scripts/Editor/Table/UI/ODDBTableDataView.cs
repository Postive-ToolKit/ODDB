using System;
using Plugins.ODDB.Scripts.Runtime.Data.Enum;
using TeamODD.ODDB.Editors.UI;
using TeamODD.ODDB.Scripts.Runtime.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    [UxmlElement]
    public partial class ODDBTableDataView : VisualElement
    {
        private TextField _tableNameInput;
        private TextField _tableKeyInput;

        #region ToolBox
        private GroupBox _toolBox;
        private Toggle _tableAutoWidthToggle;
        private Button _createRowButton;
        #endregion

        private ScrollView _multiColumnContainer;
        private ODDBMultiColumnListView _multiColumnListView;

        public event Action<ODDBTable> OnTableNameChanged;
        
        private ODDBTable _table;
        public ODDBTableDataView()
        {
            style.flexGrow = 1;
            style.flexShrink = 0;
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Stretch; // FlexStart에서 Stretch로 변경

            BuildInfoBox();
            BuildToolBox();

            _multiColumnContainer = new ScrollView();
            //multiColumnContainer.style.flexGrow = 1;
            _multiColumnContainer.mode = ScrollViewMode.VerticalAndHorizontal;
            _multiColumnListView = new ODDBMultiColumnListView();
            _multiColumnContainer.RegisterCallback<GeometryChangedEvent> (evt =>
            {
                if (_tableAutoWidthToggle.value)
                {
                    var currentWidth = _multiColumnContainer.resolvedStyle.width;
                    _multiColumnListView.UpdateMaxWidth(currentWidth);
                    return;
                }
                _multiColumnListView.UpdateMaxWidth();
            });
            _multiColumnContainer.Add(_multiColumnListView);
            
            Add(_multiColumnContainer);
        }
        private void BuildInfoBox()
        {
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


        }
        private void BuildToolBox()
        {
            _toolBox = new GroupBox();
            _toolBox.style.flexShrink = 1;
            _toolBox.style.flexDirection = FlexDirection.Row;
            _toolBox.style.paddingBottom = 0;
            _toolBox.style.paddingTop = 0;
            _toolBox.style.paddingLeft = 0;
            _toolBox.style.paddingRight = 0;
            _toolBox.style.marginBottom = 0;
            _toolBox.style.marginTop = 0;
            _toolBox.style.marginLeft = 0;
            _toolBox.style.marginRight = 0;
            
            // add button to create new row
            _createRowButton = new Button();
            _createRowButton.text = "Create Row";
            _createRowButton.clicked += OnAddRowClicked;
            _createRowButton.style.flexGrow = 0;
            _createRowButton.style.flexShrink = 1;
            _toolBox.Add(_createRowButton);
            
            _tableAutoWidthToggle = new Toggle();
            _tableAutoWidthToggle.value = true;
            _tableAutoWidthToggle.text = "Auto Width";
            _tableAutoWidthToggle.style.flexGrow = 0;
            _tableAutoWidthToggle.style.flexShrink = 1;
            _tableAutoWidthToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                if (_table == null)
                    return;
                if (evt.newValue)
                {
                    var currentWidth = _multiColumnContainer.resolvedStyle.width;
                    _multiColumnListView.UpdateMaxWidth(currentWidth);
                    return;
                }
                _multiColumnListView.UpdateMaxWidth();
                
            });
            _toolBox.Add(_tableAutoWidthToggle);
            
            Add(_toolBox);
            _toolBox.SetEnabled(false);
        }

        private void OnAddRowClicked()
        {
            if (_table == null)
                return;
            _table.AddRow();
            _multiColumnListView.IsDirty = true;
        }

        public void SetTable(ODDBTable table)
        {
            _table = table;
            if (table == null) {
                _toolBox.SetEnabled(false);
                _tableNameInput.SetEnabled(false);
                _tableNameInput.value = string.Empty;
                _tableKeyInput.value = string.Empty;
                return;
            }
            _toolBox.SetEnabled(true);
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
