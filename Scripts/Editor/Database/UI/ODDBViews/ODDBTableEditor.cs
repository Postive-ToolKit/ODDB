using System;
using TeamODD.ODDB.Editors.DTO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Enum;
using TeamODD.ODDB.Runtime.Utils;
using UnityEditor.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDBTableEditor : ODDBMultiColumnEditor, IODDBGeometryUpdate
    {
        private ODDBTable _table;
        private const float DELETE_COLUMN_WIDTH = 30f;
        private readonly IODDBEditorUseCase _editorUseCase;
        private TableDataDTO _tableDataDTO;
        public ODDBTableEditor()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            selectionType = SelectionType.Single;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            showBorder = true;
            horizontalScrollingEnabled = true;
            style.flexGrow = 1;
            bindingPath = "Rows";
            CreateColumns();
        }
        
        public override void SetView(string viewKey)
        {
            var view = _editorUseCase.GetViewByKey(viewKey);
            if (view is not ODDBTable table)
                return;
            _table = table;
            
            _tableDataDTO = new TableDataDTO();
            _tableDataDTO.Rows = _table.Rows;
            var so = new SerializedObject(_tableDataDTO);
            this.Bind(so);
            CreateColumns();
        }

        private void CreateColumns()
        {
            if (_table == null) 
                return;

            columns.Clear();
            columns.Add(new Column() {bindingPath = ODDBRow.ID_FIELD, title = "ID", stretchable = false, minWidth = 80});
            for (int i = 0; i < _table.TotalFields.Count; i++)
            {
                var columnIndex = i; // Capture the current index for the closure
                var meta = _table.TotalFields[i];
                var columnName = $"{meta.Name}[{meta.Type}]";
                var column = new Column()
                {
                    bindingPath = $"{ODDBRow.CELLS_FIELD}.Array.data[{columnIndex}]",
                    title = columnName,
                    stretchable = true,
                };
                column.bindHeader = (element) => BindHeaderElement(element, column, meta, columnIndex);
                columns.Add(column);
            }
            
            columns.Add(CreateToolColumn());
            // 컬럼 변경 후 width 갱신
        }
        
        private void BindHeaderElement(VisualElement element, Column column, ODDBField meta,int columnIndex)
        {
            var header = element as Label;
            header!.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 1)
                    return;
                evt.StopPropagation();
                //create context menu
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent($"field id : {meta.ID}"), false, null);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Delete"), false, () =>
                {
                    _table.RemoveField(columnIndex);
                    IsDirty = true;
                });
                menu.AddItem(new GUIContent("Change Name"), false, () =>
                {
                    // Show a dialog to change the name
                    var changeNameDialog = new ODDBStringInputWindow.Builder();
                    changeNameDialog.SetTitle("Change Field Name");
                    changeNameDialog.SetOnConfirm(newName =>
                    {
                        _table.TotalFields[columnIndex].Name = newName;
                        column.title =  $"{_table.TotalFields[columnIndex].Name}[{_table.TotalFields[columnIndex].Type}]";;
                    });
                    changeNameDialog.Build();
                });
                
                menu.ShowAsContext();
            });
        }
        
        private Column CreateToolColumn()
        {
            var toolColumn = new Column()
            {
                title = "",
                name = "DeleteColumn",
                maxWidth = DELETE_COLUMN_WIDTH,
                width = DELETE_COLUMN_WIDTH,
                minWidth = DELETE_COLUMN_WIDTH,
                stretchable = false,
                resizable = false
            };
            // field will work like add field button
            toolColumn.makeHeader = () =>
            {
                var header = new Label()
                {
                    text = "+",
                    style =
                    {
                        flexGrow = 1,
                        flexShrink = 0,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                };
                return header;
            };

            toolColumn.bindHeader = (element) =>
            {
                var header = element as Label;
                header!.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0)
                        return;
                    OnAddTableColumnClicked();
                });
            };
            
            // row will work like delete row button
            toolColumn.makeCell = () =>
            {
                var button = new Button()
                {
                    text = "-",
                    style =
                    {
                        flexGrow = 0,
                        flexShrink = 0,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                };
                return button;
            };

            toolColumn.bindCell = (element, index) =>
            {
                var button = element as Button;
                button.RegisterCallbackOnce<ClickEvent> (evt => 
                {
                    evt.StopPropagation();
                    OnButtonClicked();
                });
                
                void OnButtonClicked()
                {
                    if (_table != null && index < _table.Rows.Count)
                    {
                        _table.RemoveRow(index);
                        IsDirty = true;
                    }
                }
            };

            return toolColumn;
        }
        
        private void OnAddTableColumnClicked()
        {
            if (_table == null)
                return;
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
                        _table.AddField(new ODDBField(new ODDBID(), input, dataType));
                        IsDirty = true;
                    });
                    dialogBuilder.Build();
                });
            }
            // 메뉴 표시
            menu.ShowAsContext();
        }
        
        public void UpdateGeometry(GeometryChangedEvent evt)
        {
            var maxWidth = evt.newRect.width;
            if (maxWidth > 0)
            {
                style.maxWidth = maxWidth;
                return;
            }
            float totalWidth = 0;
            foreach (var column in columns)
            {
                totalWidth += column.maxWidth.value;
            }
            //style.width = totalWidth;
            style.maxWidth = totalWidth;
        }
    }
}
