using System;
using TeamODD.ODDB.Editors.DTO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Utils.Converters;
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
                    bindingPath = $"{Row.CELLS_FIELD}.Array.data[{columnIndex}]",
                    title = columnName,
                    stretchable = true,
                    resizable = true,
                    minWidth = 80,
                };
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
    }
}
