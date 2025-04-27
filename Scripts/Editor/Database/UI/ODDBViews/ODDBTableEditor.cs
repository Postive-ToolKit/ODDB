using System;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TeamODD.ODDB.Editors.UI.Fields;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Enum;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDBTableEditor : ODDBMultiColumnEditor, IODDBGeometryUpdate
    {
        private ODDBTable _table;
        private List<string> _columnNames = new List<string>();
        private const float DELETE_COLUMN_WIDTH = 30f;
        private readonly IODDBEditorUseCase _editorUseCase;
        public ODDBTableEditor()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            selectionType = SelectionType.Single;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            showBorder = true;
            horizontalScrollingEnabled = false;
            style.flexShrink = 1;

            schedule.Execute(Update).Every(100);
        }

        private void Update()
        {
            if (IsDirty)
            {
                IsDirty = false;
                RefreshColumns();
                RefreshItems();
                Rebuild();
            }
        }

        public override void SetView(string viewKey)
        {
            var view = _editorUseCase.GetViewByKey(viewKey);
            if (view is not ODDBTable table)
                return;
            _table = table;
            RefreshColumns();
            RefreshItems();
        }

        private void RefreshColumns()
        {
            if (_table == null) return;

            columns.Clear();
            _columnNames.Clear();
            
            columns.Add(CreateKeyColumn());

            // add data columns
            for (int i = 0; i < _table.TotalFields.Count; i++)
            {
                var meta = _table.TotalFields[i];
                var columnName = $"{meta.Name}[{meta.Type}]";
                _columnNames.Add(columnName);

                var column = new Column()
                {
                    title = columnName,
                    name = columnName,
                    maxWidth = 300,
                    width = 60,
                    minWidth = 60,
                    stretchable = true,
                    resizable = true,
                };

                var dataType = meta.Type;
                var columnIndex = i;
                column.makeHeader = () => CreateHeaderElement(meta);
                column.bindHeader = (element) => BindHeaderElement(element, column, meta, columnIndex);
                column.makeCell = () => CreateCell(dataType);
                column.bindCell = (element, index) => BindCell(element, index, columnIndex);

                columns.Add(column);
            }
            
            columns.Add(CreateToolColumn());
        }

        private Column CreateKeyColumn()
        {
            var keyColumn = new Column()
            {
                title = "Key",
                name = "Key",
                maxWidth = 100,
                minWidth = 100,
                stretchable = true,
                resizable = true
            };
            keyColumn.makeCell =
                () => new TextField()
                {
                    style =
                    {
                        flexShrink = 1,
                        unityTextAlign = TextAnchor.MiddleLeft
                    },
                    isReadOnly = true,
                };
            keyColumn.bindCell = (element, index) =>
            {
                if (index >= _table.ReadOnlyRows.Count)
                    return;
                var container = element as TextField;
                container!.value = _table.ReadOnlyRows[index].Key;
            };
            return keyColumn;
        }

        private VisualElement CreateHeaderElement(ODDBField field)
        {
            var columnTitle = $"{field.Name}[{field.Type}]";
            var header = new Label(columnTitle) {
                style =
                {
                    flexGrow = 1,
                    paddingLeft = 8,
                    unityTextAlign = TextAnchor.MiddleLeft,
                }
            };
            return header;
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
                        column.title = newName;
                        _columnNames[columnIndex] = newName;
                        IsDirty = true;
                    });
                    changeNameDialog.Build();
                });
                
                menu.ShowAsContext();
            });
        }
        private VisualElement CreateCell(ODDBDataType dataType)
        {
            var container = new ODDBFieldBase();
            var field = ODDBFieldFactory.CreateField(dataType);
            container.Add(field.Root);
            container.userData = field;
            return container;
        }

        private void BindCell(VisualElement element, int index, int columnIndex)
        {
            var container = element as VisualElement;
            var field = container.userData as IODDBField;
            
            if (field != null)
            {
                var value = _table.GetValue(index, columnIndex);
                field.SetValue(value);
                field.RegisterValueChangedCallback(newValue =>
                {
                    if (_table != null && index < _table.ReadOnlyRows.Count)
                    {
                        var row = _table.ReadOnlyRows[index];
                        row.SetData(columnIndex, newValue.ToString());
                    }
                });
            }
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
                        flexGrow = 0,
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
                if (button != null)
                {
                    button.clicked += () =>
                    {
                        if (_table != null && index < _table.ReadOnlyRows.Count)
                        {
                            _table.RemoveRow(index);
                            IsDirty = true;
                        }
                    };
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
        
        private new void RefreshItems()
        {
            if (_table == null) return;
            
            itemsSource = new List<int>();
            for (int i = 0; i < _table.ReadOnlyRows.Count; i++)
            {
                (itemsSource as List<int>).Add(i);
            }
            Rebuild();
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
