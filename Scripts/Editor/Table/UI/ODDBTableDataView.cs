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
        
        private Button _createRowButton;
        private Button _addTableColumnButton;
        private Button _removeTableColumnButton;
        
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

            var multiColumnContainer = new ScrollView();
            multiColumnContainer.style.flexGrow = 1;
            multiColumnContainer.mode = ScrollViewMode.VerticalAndHorizontal;
            
            _multiColumnListView = new ODDBMultiColumnListView();

            multiColumnContainer.Add(_multiColumnListView);
            
            Add(multiColumnContainer);
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
            var toolBox = new GroupBox();
            toolBox.style.flexShrink = 1;
            toolBox.style.flexDirection = FlexDirection.Row;
            toolBox.style.paddingBottom = 0;
            toolBox.style.paddingTop = 0;
            toolBox.style.paddingLeft = 0;
            toolBox.style.paddingRight = 0;
            toolBox.style.marginBottom = 0;
            toolBox.style.marginTop = 0;
            toolBox.style.marginLeft = 0;
            toolBox.style.marginRight = 0;
            
            // add button to create new row
            _createRowButton = new Button();
            _createRowButton.text = "Create Row";
            _createRowButton.clicked += OnAddRowClicked;
            _createRowButton.style.flexGrow = 0;
            _createRowButton.style.flexShrink = 1;
            toolBox.Add(_createRowButton);
            
            // add button to add field
            _addTableColumnButton = new Button();
            _addTableColumnButton.text = "Add Field";
            _addTableColumnButton.style.flexGrow = 0;
            _addTableColumnButton.style.flexShrink = 1;
            _addTableColumnButton.clicked += OnAddTableColumnClicked;
            toolBox.Add(_addTableColumnButton);
            

            Add(toolBox);
        }

        private void OnAddTableColumnClicked()
        {
            if (_table == null)
                return;
            // _table.AddColumn();
            // _multiColumnListView.RefreshColumns();
            // _multiColumnListView.IsDirty = true;
            // create context menu
            var menu = new GenericMenu();
            
            foreach (ODDBDataType dataType in Enum.GetValues(typeof(ODDBDataType)))
            {
                menu.AddItem(new GUIContent(dataType.ToString()), false, () => {
                    if (_table == null)
                        return;
                    var dialogBuilder = new ODDBStringInputWindow.Builder();
                    dialogBuilder.SetTitle("Please enter field name");
                    dialogBuilder.SetOnConfirm((input) =>
                    {
                        _table.AddField(new ODDBTableMeta(dataType, input));
                        _multiColumnListView.IsDirty = true;
                    });
                    dialogBuilder.Build();
                });
            }
            // 메뉴 표시
            menu.ShowAsContext();
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
