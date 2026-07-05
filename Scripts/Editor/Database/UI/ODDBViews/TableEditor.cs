using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TeamODD.ODDB.Editors.PropertyDrawers;
using TeamODD.ODDB.Editors.UI.Dialogs;
using TeamODD.ODDB.Editors.Utils.Elements;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class TableEditor : MultiColumnEditor
    {
        private const float DELETE_COLUMN_WIDTH = 30f;
        private readonly IODDBEditorUseCase _editorUseCase;
        private Table _table;

        public TableEditor()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            selectionType = SelectionType.Single;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            horizontalScrollingEnabled = true;
            showBorder = true;
            style.flexGrow = 1;
            style.height = Length.Percent(100);
            CreateColumns();
        }

        public override void SetView(string viewKey)
        {
            if (_table != null)
            {
                _table.OnRowChanged -= RefreshRows;
                _table.OnFieldsChanged -= CreateColumns;
                _editorUseCase.OnViewChanged -= OnExternalViewChanged;
            }
            var view = _editorUseCase.GetViewByKey(viewKey);
            if (view is not Table table)
                return;
            _table = table;

            itemsSource = _table.Rows;
            CreateColumns();
            RefreshRows();

            _table.OnRowChanged += RefreshRows;
            _table.OnFieldsChanged += CreateColumns;
            // External (MCP) cell mutations don't fire OnRowChanged; subscribe
            // to the use case's view-changed signal so the table refreshes.
            _editorUseCase.OnViewChanged += OnExternalViewChanged;
        }

        private void OnExternalViewChanged(string viewId)
        {
            if (_table == null || viewId != _table.ID) return;
            RefreshRows();
        }

        private void RefreshRows()
        {
            if (_table == null)
                return;
            itemsSource = _table.Rows;
            RefreshItems();
        }

        private void CreateColumns()
        {
            if (_table == null)
                return;
            columns.Clear();
            columns.Add(CreateIdColumn());
            for (int i = 0; i < _table.TotalFields.Count; i++)
            {
                try { columns.Add(CreateCellColumn(i)); }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[ODDB] Failed to build column {i} for table {_table.Name}: {ex.Message}");
                    columns.Add(new Column { title = $"<broken {i}>", width = 80 });
                }
            }
            columns.Add(CreateToolColumn());
        }

        private Column CreateIdColumn()
        {
            var column = new Column()
            {
                title = "ID",
                maxWidth = 80,
                width = 80,
            };
            column.makeCell = () => new Label()
            {
                style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 4 }
            };
            column.bindCell = (element, index) =>
            {
                if (_table == null || index < 0 || index >= _table.Rows.Count) return;
                var label = (Label)element;
                var rowId = _table.Rows[index].ID.ToString();
                label.text = rowId;
                label.tooltip = "Right-click to change row ID";
                label.userData = rowId;
                label.UnregisterCallback<ContextClickEvent>(OnRowIdContextClick);
                label.RegisterCallback<ContextClickEvent>(OnRowIdContextClick);
            };
            return column;
        }

        private void OnRowIdContextClick(ContextClickEvent evt)
        {
            if (_table == null || evt.currentTarget is not Label label || label.userData is not string rowId)
                return;

            var capturedTableId = _table.ID.ToString();
            var capturedRowId = rowId;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Change ID..."), false,
                () => ODDBChangeIdWindow.ShowForRow(_editorUseCase, capturedTableId, capturedRowId));
            menu.ShowAsContext();
            evt.StopPropagation();
        }

        private Column CreateCellColumn(int columnIndex)
        {
            var meta = _table.TotalFields[columnIndex];
            if (meta.Type == null) meta.Type = new FieldType();
            var columnName = $"{meta.Name}[{EditorDataTypeExtensions.GetDisplayName(meta.Type.TypeKey, meta.Type.Param)}]";
            var column = new Column()
            {
                title = columnName,
                stretchable = true,
                resizable = true,
                minWidth = 80,
            };
            column.makeHeader = () => CreateColumnHeader(columnIndex);
            column.makeCell = () => new VisualElement()
            {
                style = { flexGrow = 1, justifyContent = Justify.Center }
            };
            column.bindCell = (element, index) =>
            {
                element.Clear();
                if (_table == null || index < 0 || index >= _table.Rows.Count) return;
                if (columnIndex >= _table.TotalFields.Count) return;

                var row = _table.Rows[index];
                var cell = row.GetData(columnIndex);
                if (cell == null) return;

                var fieldType = _table.TotalFields[columnIndex].Type;
                var typeKey = fieldType?.TypeKey ?? string.Empty;
                var param = fieldType?.Param ?? string.Empty;

                var drawer = CellDrawerRegistry.Get(typeKey);
                if (drawer == null)
                {
                    element.Add(new Label($"<no drawer for '{typeKey}'>"));
                    return;
                }

                var capturedRowId = row.ID.ToString();
                var capturedColumn = columnIndex;
                var gui = drawer.CreatePropertyGUI(cell, typeKey, param, newSerialized =>
                {
                    if (_table == null) return;
                    _editorUseCase.SetCellData(_table.ID, capturedRowId, capturedColumn, newSerialized, direct: true);
                });
                element.Add(gui);
            };
            return column;
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

            toolColumn.makeCell = () => new ODDBButton() { text = "-", };

            toolColumn.bindCell = (element, index) =>
            {
                if (_table == null || index < 0 || index >= _table.Rows.Count)
                    return;
                var row = _table.Rows.ElementAt(index);
                var button = element as ODDBButton;
                button!.ClearCallbacks();
                button.AddOnClickCallback(evt =>
                {
                    if (_table == null)
                        return;
                    _editorUseCase.RemoveRow(_table.ID, row.ID);
                });
            };

            return toolColumn;
        }

        private VisualElement CreateColumnHeader(int columnIndex)
        {
            if (_table == null || columnIndex < 0 || columnIndex >= _table.TotalFields.Count)
                return new Label("Invalid Column");
            var meta = _table.TotalFields[columnIndex];
            if (meta.Type == null) meta.Type = new FieldType();

            var container = new VisualElement()
            {
                style =
                {
                    flexGrow = 1, flexDirection = FlexDirection.Column, alignItems = Align.Center, justifyContent = Justify.Center,
                }
            };
            var label = new Label() { style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleCenter, flexGrow = 1, }, };
            label.text = meta.Name;
            container.Add(label);

            var type = new Label() { style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleCenter, flexGrow = 1, }, };
            type.text = EditorDataTypeExtensions.GetDisplayName(meta.Type.TypeKey, meta.Type.Param);
            container.Add(type);

            var bindField = new Label() { style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleCenter, flexGrow = 1, }, };
            bindField.text = GetBindTypeFieldName(columnIndex);
            container.Add(bindField);
            return container;
        }

        private string GetBindTypeFieldName(int columnIndex)
            => GetBindTypeFieldName(_table?.BindType, columnIndex);

        public static string GetBindTypeFieldName(System.Type bindType, int columnIndex)
        {
            if (bindType == null)
                return string.Empty;

            var allFields = new List<FieldInfo>();

            var currentType = bindType;
            while (currentType != null
                   && currentType != typeof(object)
                   && currentType != typeof(ODDBEntity))
            {
                var fields = currentType
                    .GetFields(ODDBEntity.FieldFlags)
                    .Where(f => f.IsDefined(typeof(CompilerGeneratedAttribute), false) == false);

                allFields.InsertRange(0, fields);
                currentType = currentType.BaseType;
            }

            if (columnIndex < 0 || columnIndex >= allFields.Count)
                return string.Empty;

            var field = allFields[columnIndex];

            var inspectorAttr = field.GetCustomAttribute<InspectorNameAttribute>();
            if (inspectorAttr != null && !string.IsNullOrEmpty(inspectorAttr.displayName))
                return inspectorAttr.displayName;

            return ObjectNames.NicifyVariableName(field.Name);
        }
    }
}
