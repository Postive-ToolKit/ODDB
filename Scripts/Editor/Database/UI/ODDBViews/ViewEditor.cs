using System.Collections.Generic;
using TeamODD.ODDB.Editors.DTO;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ViewEditor : MultiColumnEditor
    {
        private IView _view;
        private const float DELETE_COLUMN_WIDTH = 30f;
        private readonly IODDBEditorUseCase _editorUseCase;
        private ViewDataDTO _viewDataDto;

        public ViewEditor()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            selectionType = SelectionType.Single;
            showBorder = true;
            horizontalScrollingEnabled = false;
            style.flexGrow = 1;
            bindingPath = "Fields";
            columns.Add(new Column() {bindingPath = "ID", title = "ID", stretchable = false, minWidth = 80});
            columns.Add(new Column() {bindingPath = "Name", title = "Name", stretchable = true});
            columns.Add(new Column() {bindingPath = "Type", title = "Type", stretchable = true});
            columns.Add(CreateToolColumn());
        }
        public override void SetView(string viewKey)
        {
            _view = _editorUseCase.GetViewByKey(viewKey);
            if (_view == null) {
                return;
            }
            
            _viewDataDto = ScriptableObject.CreateInstance<ViewDataDTO>();
            _viewDataDto.Fields = _view.ScopedFields;
            _viewDataDto.OnFieldsChanged += _view.NotifyFieldsChanged;
            var so = new SerializedObject(_viewDataDto);
            this.Bind(so);
        }

        private Column CreateToolColumn()
        {
            var toolColumn = new Column()
            {
                title = "",
                name = "",
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
                if (button != null)
                {
                    button.clicked += () =>
                    {
                        if (_view != null && index < _view.TotalFields.Count)
                        {
                            _view.RemoveField(index);
                        }
                    };
                }
            };

            return toolColumn;
        }
    }
}