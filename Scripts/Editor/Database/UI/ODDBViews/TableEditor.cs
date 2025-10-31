using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Editors.DTO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using UnityEditor.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class TableEditor : MultiColumnEditor
    {
        private const float DELETE_COLUMN_WIDTH = 30f;
        private readonly IODDBEditorUseCase _editorUseCase;
        private TableDataDTO _tableDataDTO;
        private Table _table;
        public TableEditor()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            selectionType = SelectionType.Single;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            horizontalScrollingEnabled = true;
            showBorder = true;
            style.flexGrow = 1;
            bindingPath = "Rows";
            CreateColumns();
        }
        
        public override void SetView(string viewKey)
        {
            var view = _editorUseCase.GetViewByKey(viewKey);
            if (view is not Table table)
                return;
            
            this.Unbind();
            _table = table;
            _tableDataDTO = ScriptableObject.CreateInstance<TableDataDTO>();
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
            columns.Add(new Column() {bindingPath = Row.ID_FIELD, title = "ID", maxWidth = 80, width = 80});
            for (int i = 0; i < _table.TotalFields.Count; i++)
            {
                var columnIndex = i; // Capture the current index for the closure
                var meta = _table.TotalFields[i];
                var columnName = $"{meta.Name}[{meta.Type}]";
                var column = new Column()
                {
                    title = columnName,
                    bindingPath = $"{Row.CELLS_FIELD}.Array.data[{columnIndex}]",
                    stretchable = true,
                    resizable = true,
                    minWidth = 80,
                };
                column.makeHeader = () => CreateColumnHeader(columnIndex);
                
                columns.Add(column);
            }
            
            columns.Add(CreateToolColumn());
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
                if (button == null)
                    return;
                
                button.clicked -= button.userData as Action; // 기존 핸들러 제거
                Action handler = () =>
                {
                    if (_table != null && index < _table.Rows.Count)
                        _table.RemoveRow(index);
                };
                button.userData = handler; // 핸들러를 userData에 저장
                button.clicked += handler; // 새 핸들러 등록
            };

            return toolColumn;
        }
        
        private VisualElement CreateColumnHeader(int columnIndex)
        {
            if (_table == null || columnIndex < 0 || columnIndex >= _table.TotalFields.Count)
                return new Label("Invalid Column");
            var meta = _table.TotalFields[columnIndex];
            
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
            type.text = meta.Type.ToString();
            container.Add(type);
            
            var bindField = new Label() { style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleCenter, flexGrow = 1, }, };
            bindField.text = GetBindTypeFieldName(columnIndex);
            Debug.Log(GetBindTypeFieldName(columnIndex));
            container.Add(bindField);
            return container;
        }

        private string GetBindTypeFieldName(int columnIndex)
        {
            if (_table.BindType == null)
                return string.Empty;

            var bindType = _table.BindType;
            var allFields = new List<FieldInfo>();

            // Traverse the inheritance chain to collect all instance fields
            var currentType = bindType;
            while (currentType != null && currentType != typeof(object))
            {
                var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                allFields.AddRange(fields);
                currentType = currentType.BaseType;
            }

            // Filter only Unity-serializable fields (public or with [SerializeField])
            var serializableFields = allFields
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                .OrderBy(f => f.MetadataToken)
                .ToArray();

            if (columnIndex < 0 || columnIndex >= serializableFields.Length)
                return string.Empty;

            var field = serializableFields[columnIndex];
            
            // Check for InspectorNameAttribute first
            var inspectorAttr = field.GetCustomAttribute<InspectorNameAttribute>();
            if (inspectorAttr != null && !string.IsNullOrEmpty(inspectorAttr.displayName))
                return inspectorAttr.displayName;

            // Use Unity's NicifyVariableName
            return ObjectNames.NicifyVariableName(field.Name);
        }
    }
}
