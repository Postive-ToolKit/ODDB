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
        private string _viewId;
        private bool _isCommittingCell;
        private bool _rowRefreshHandled;
        private bool _isSubscribed;

        private sealed class ReusableCellHost : VisualElement
        {
            public string RowId { get; set; }
            public VisualElement Content { get; set; }
        }

        public TableEditor()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            selectionType = SelectionType.Single;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            horizontalScrollingEnabled = true;
            showBorder = true;
            style.flexGrow = 1;
            style.height = Length.Percent(100);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            CreateColumns();
        }

        public override void SetView(string viewKey)
        {
            Unsubscribe();
            _viewId = viewKey;
            var view = _editorUseCase.GetViewByKey(viewKey);
            if (view is not Table table)
            {
                _table = null;
                itemsSource = null;
                columns.Clear();
                RefreshItems();
                return;
            }
            _table = table;

            itemsSource = _table.Rows;
            CreateColumns();
            RefreshRows();

            Subscribe();
        }

        private void OnExternalViewChanged(string viewId)
        {
            if (string.IsNullOrEmpty(viewId))
            {
                SetView(_viewId);
                return;
            }
            if (_isCommittingCell || _table == null || viewId != _table.ID) return;
            if (_rowRefreshHandled)
            {
                _rowRefreshHandled = false;
                return;
            }
            RefreshRows();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt) => Subscribe();

        private void OnDetachFromPanel(DetachFromPanelEvent evt) => Unsubscribe();

        private void Subscribe()
        {
            if (_isSubscribed || panel == null || _table == null || _editorUseCase == null) return;
            _table.OnRowChanged += OnRowsChanged;
            _table.OnFieldsChanged += CreateColumns;
            _editorUseCase.OnViewChanged += OnExternalViewChanged;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;
            if (_table != null)
            {
                _table.OnRowChanged -= OnRowsChanged;
                _table.OnFieldsChanged -= CreateColumns;
            }
            if (_editorUseCase != null)
                _editorUseCase.OnViewChanged -= OnExternalViewChanged;
            _isSubscribed = false;
        }

        private void OnRowsChanged()
        {
            // AddRow/RemoveRow raise OnRowChanged and then synchronously publish
            // OnViewChanged. Refresh here and consume that duplicate notification.
            _rowRefreshHandled = true;
            schedule.Execute(() => _rowRefreshHandled = false);
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
            column.makeCell = () =>
            {
                var label = new Label
                {
                    style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 4 }
                };
                label.RegisterCallback<ContextClickEvent>(OnRowIdContextClick);
                return label;
            };
            column.bindCell = (element, index) =>
            {
                var label = (Label)element;
                label.userData = null;
                label.text = string.Empty;
                if (_table == null || index < 0 || index >= _table.Rows.Count) return;
                var rowId = _table.Rows[index].ID.ToString();
                label.text = rowId;
                label.tooltip = "Right-click to change row ID";
                label.userData = rowId;
            };
            column.unbindCell = (element, _) => element.userData = null;
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
            var typeKey = meta.Type.TypeKey ?? string.Empty;
            var param = meta.Type.Param ?? string.Empty;
            var drawer = CellDrawerRegistry.Get(typeKey);
            var reusableDrawer = drawer as IODDBReusableCellDrawer;
            var columnName = $"{meta.Name}[{EditorDataTypeExtensions.GetDisplayName(typeKey, param)}]";
            var column = new Column()
            {
                title = columnName,
                stretchable = true,
                resizable = true,
                minWidth = 80,
            };
            column.makeHeader = () => CreateColumnHeader(columnIndex);
            column.makeCell = () =>
            {
                var host = new ReusableCellHost
                {
                    style = { flexGrow = 1, justifyContent = Justify.Center }
                };
                if (reusableDrawer == null)
                    return host;

                host.Content = reusableDrawer.CreateReusablePropertyGUI(
                    typeKey,
                    param,
                    serialized => CommitCell(host, columnIndex, serialized));
                host.Add(host.Content);
                return host;
            };
            column.bindCell = (element, index) =>
            {
                var host = (ReusableCellHost)element;
                host.RowId = null;
                if (host.Content != null)
                    host.Content.style.display = DisplayStyle.None;
                else
                    host.Clear();
                if (_table == null || index < 0 || index >= _table.Rows.Count) return;
                if (columnIndex >= _table.TotalFields.Count) return;

                var row = _table.Rows[index];
                var cell = row.GetData(columnIndex);
                if (cell == null) return;

                if (drawer == null)
                {
                    host.Clear();
                    host.Add(new Label($"<no drawer for '{typeKey}'>"));
                    return;
                }

                host.RowId = row.ID.ToString();
                if (reusableDrawer != null)
                {
                    host.Content.style.display = DisplayStyle.Flex;
                    reusableDrawer.BindPropertyGUI(host.Content, cell, typeKey, param);
                    return;
                }

                host.Clear();
                host.Add(drawer.CreatePropertyGUI(
                    cell,
                    typeKey,
                    param,
                    serialized => CommitCell(host, columnIndex, serialized)));
            };
            column.unbindCell = (element, _) =>
            {
                var host = (ReusableCellHost)element;
                host.RowId = null;
                if (host.Content != null)
                    host.Content.style.display = DisplayStyle.None;
                else
                    host.Clear();
            };
            return column;
        }

        private void CommitCell(ReusableCellHost host, int columnIndex, string serialized)
        {
            if (_table == null || string.IsNullOrEmpty(host.RowId)) return;
            try
            {
                // SetCellData raises OnViewChanged synchronously. The edited control
                // already contains the new value, so refreshing here would destroy
                // focus and defeat list virtualization.
                _isCommittingCell = true;
                _editorUseCase.SetCellData(
                    _table.ID,
                    host.RowId,
                    columnIndex,
                    serialized,
                    direct: true);
            }
            finally
            {
                _isCommittingCell = false;
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

            toolColumn.makeCell = () =>
            {
                var button = new ODDBButton { text = "-" };
                button.AddOnClickCallback(OnDeleteRowClicked);
                return button;
            };

            toolColumn.bindCell = (element, index) =>
            {
                element.userData = null;
                if (_table == null || index < 0 || index >= _table.Rows.Count)
                    return;
                var row = _table.Rows.ElementAt(index);
                element.userData = row.ID.ToString();
            };
            toolColumn.unbindCell = (element, _) => element.userData = null;

            return toolColumn;
        }

        private void OnDeleteRowClicked(ClickEvent evt)
        {
            if (_table == null
                || evt.currentTarget is not ODDBButton button
                || button.userData is not string rowId)
                return;

            _editorUseCase.RemoveRow(_table.ID, rowId);
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
