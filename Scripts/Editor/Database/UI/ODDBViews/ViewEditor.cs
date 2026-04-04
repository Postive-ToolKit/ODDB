using System;
using Plugins.ODDB.Scripts.Editor.Utils.Elements;
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
            selectionType = SelectionType.Single;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            reorderable = true;
            itemIndexChanged += (oldIndex, newIndex) =>
            {
                if (_view == null) return;

                // ListView already reordered the underlying list (_view.ScopedFields).
                // We need to revert it temporarily so the Command can perform the move and handle Undo properly.
                var list = _view.ScopedFields;
                var item = list[newIndex];
                list.RemoveAt(newIndex);
                list.Insert(oldIndex, item);

                int myStartIndex = 0;
                if (_view.ParentView != null)
                    myStartIndex = _view.ParentView.TotalFields.Count;
                
                _editorUseCase.MoveField(_view.ID, oldIndex + myStartIndex, newIndex + myStartIndex);
            };
            horizontalScrollingEnabled = true;
            showBorder = true;
            style.flexGrow = 1;
            bindingPath = "Fields";
            
            columns.Add(CreateHandleColumn());
            columns.Add(new Column() {bindingPath = "Name", title = "Name", stretchable = true});
            columns.Add(new Column() {bindingPath = "Type", title = "Type", stretchable = true});
            columns.Add(CreateToolColumn());
        }

        private Column CreateHandleColumn()
        {
            var handleColumn = new Column()
            {
                title = "",
                maxWidth = 25f,
                width = 25f,
                minWidth = 25f,
                stretchable = false,
                resizable = false
            };

            handleColumn.makeCell = () =>
            {
                var container = new VisualElement();
                container.style.justifyContent = Justify.Center;
                container.style.alignItems = Align.Center;
                container.style.flexGrow = 1;

                var icon = new VisualElement();
                icon.style.width = 15;
                icon.style.height = 15;
                // Unity built-in handle icon
                icon.style.backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image;
                icon.style.opacity = 0.5f; // Slightly transparent to look subtle
                
                container.Add(icon);
                return container;
            };

            // 빈 bindCell/unbindCell을 추가하여 바인딩 시스템 에러 방지
            handleColumn.bindCell = (element, index) => { };
            handleColumn.unbindCell = (element, index) => { };

            return handleColumn;
        }
        public override void SetView(string viewKey)
        {
            _view = _editorUseCase.GetViewByKey(viewKey);
            if (_view == null) {
                return;
            }
            this.Unbind();
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
            toolColumn.makeCell = () => new ODDBButton() { text = "-", };
            
            toolColumn.bindCell = (element, index) =>
            {
                var button = element as ODDBButton;
                button!.ClearCallbacks();
                button.AddOnClickCallback(evt =>
                {
                    var normalizedIndex = _view.TotalFields.Count - _view.ScopedFields.Count + index;
                    if (_view != null && index < _view.TotalFields.Count)
                        _editorUseCase.RemoveField(_view.ID, normalizedIndex);
                });
            };

            return toolColumn;
        }
    }
}