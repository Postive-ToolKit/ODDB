using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    /// <summary>
    /// View model for the ODDBDataEditor
    /// </summary>
    public class ViewEditorSet : VisualElement, IHasView
    {
        private readonly List<IHasView> _viewListeners = new();
        private readonly Toolbar _toolbar;
        private readonly Toolbar _editorToolBar;
        private readonly VisualElement _contentView;
        private readonly IODDBEditorUseCase _editorUseCase;
        private IView _view;
        private ODDBViewType _type;
        
        public ViewEditorSet(string viewId)
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            //_editorUseCase.OnViewChanged += SetView;
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1;
            style.flexShrink = 0;
            style.alignContent = Align.FlexStart;
            style.paddingBottom = 0;
            style.paddingTop = 0;
            style.paddingLeft = 0;
            style.paddingRight = 0;
            style.marginBottom = 0;
            style.marginTop = 0;
            style.marginLeft = 0;
            style.marginRight = 0;

            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            
            // 이름 및 기본 정보 툴바
            _toolbar = BuildToolBox();
            Add(_toolbar);
            _editorToolBar = BuildToolBox();
            Add(_editorToolBar);
            _contentView = BuildContentView();
            Add(_contentView);
            
            SetView(viewId);
        }
        
        ~ViewEditorSet()
        {
            if (_editorUseCase != null)
                _editorUseCase.OnViewChanged -= SetView;
        }

        private Toolbar BuildToolBox()
        {
            var toolBox = new Toolbar();
            toolBox.style.flexShrink = 1;
            return toolBox;
        }
        
        private void CreateInfoEditor()
        {
            _toolbar.Clear();
            var nameButton = new ToolbarButton();
            nameButton.text = "Name";
            // 클릭 비활성화
            _toolbar.Add(nameButton);
            var nameTextField = new TextField();
            nameTextField.value = _view.Name;
            nameTextField.style.minWidth = 200;
            nameTextField.RegisterValueChangedCallback(evt =>
            {
                _editorUseCase.SetViewName(_view.ID, evt.newValue);
            });
            _toolbar.Add(nameTextField);
            
            var button = new ToolbarButton();
            button.text = "ID";
            button.RegisterCallback<ClickEvent>(evt =>
            {
                EditorGUIUtility.systemCopyBuffer = _view.ID;
                Debug.Log($"Copied ID : {_view.ID} to Clipboard");
            });
            // 클릭 비활성화
            _toolbar.Add(button);
            var textField = new TextField();
            textField.SetEnabled(false);
            textField.value = _view.ID;
            textField.isReadOnly = true;
            textField.style.flexGrow = 0;
            textField.style.flexShrink = 1;
            _toolbar.Add(textField);
            
            var editorMenu = new ToolbarMenu();
            editorMenu.text = _type.ToString();
            editorMenu.menu.AppendAction("View", _ =>
            {
                SetMode(ODDBViewType.View);
                editorMenu.text = "View";
            });
            if (_type == ODDBViewType.Table)
            {
                editorMenu.menu.AppendAction("Table", _ =>
                {
                    SetMode(ODDBViewType.Table);
                    editorMenu.text = "Table";
                });
            }
            _toolbar.Add(editorMenu);
        }
        
        private VisualElement BuildContentView()
        {
            var contentView = new VisualElement
            {
                style =
                {
                    flexGrow = 1
                }
            };
            return contentView;
        }
        
        private void AddToolBarView(ToolbarButton button)
        {
            if (button == null)
                return;
            RegisterListener(button);
            _editorToolBar.Add(button);
        }

        private void AddToolBarMenu(ToolbarMenu toolbarMenu)
        {
            if (toolbarMenu == null)
                return;
            RegisterListener(toolbarMenu);
            _editorToolBar.Add(toolbarMenu);
        }
        
        private void AddContent(VisualElement visualElement)
        {
            if (visualElement == null)
                return;
            RegisterListener(visualElement);
            _contentView.Add(visualElement);
        }
        private void RegisterListener(object listener)
        {
            if (listener is not IHasView view)
                return;
            _viewListeners.Add(view);
            view.SetView(_view.ID);
        }
        
        public void SetView(string viewKey)
        {
            // 이미 설정된 뷰와 동일하면 무시
            if (_view != null && _view.ID == viewKey)
                return;
            _view = _editorUseCase.GetViewByKey(viewKey);
            _type = _editorUseCase.GetViewTypeByKey(_view.ID);
            SetMode(_type);
            CreateInfoEditor();
            foreach (var listener in _viewListeners)
                listener.SetView(viewKey);
        }

        private void SetMode(ODDBViewType type)
        {
            _contentView.Clear();
            _editorToolBar.Clear();
            switch (type)
            {
                case ODDBViewType.View:
                    BuildViewEditor();
                    break;
                case ODDBViewType.Table:
                    BuildTableEditor();
                    break;
            }
        }

        private void BuildViewEditor()
        {
            var viewEditor = new ViewEditor();
            AddContent(viewEditor);
                
            var createRow = new ToolbarButton();
            createRow.text = "Add Metadata";
            createRow.clicked += () =>
            {
                _view.AddField(new Field());
                _editorUseCase.NotifyViewDataChanged(_view.ID);
            };
            AddToolBarView(createRow);
                
            var bindClassSelectView = new BindClassSelectView();
            bindClassSelectView.SetView(_view.ID);
                
            bindClassSelectView.OnBindClassChanged += bindType =>
            {
                _view.BindType = bindType;
                _editorUseCase.NotifyViewDataChanged(_view.ID);
            };
            AddToolBarMenu(bindClassSelectView);
                
            var inheritSelectView = new ParentViewSelectView();
            inheritSelectView.OnParentViewChanged += parentView =>
            {
                _view.ParentView = parentView;
                _editorUseCase.NotifyViewDataChanged(_view.ID);
            };
            inheritSelectView.SetView(_view.ID);
            AddToolBarView(inheritSelectView);
        }

        private void BuildTableEditor()
        {
            var table = _view as Table;
                
            var tableEditor = new TableEditor();
            AddContent(tableEditor);

                
            var createRow = new ToolbarButton();
            createRow.text = "Create Row";
            createRow.clicked += () =>
            {
                table!.AddRow();
                _editorUseCase.NotifyViewDataChanged(_view.ID);
            };
            AddToolBarView(createRow);
                
            var bindClassSelectView = new BindClassSelectView();
            bindClassSelectView.SetView(_view.ID);
            bindClassSelectView.OnBindClassChanged += bindType =>
            {
                _view.BindType = bindType;
                _editorUseCase.NotifyViewDataChanged(_view.ID);
            };
            AddToolBarMenu(bindClassSelectView);

            var inheritSelectView = new ParentViewSelectView();
            inheritSelectView.OnParentViewChanged += parentView =>
            {
                _view.ParentView = parentView;
                _editorUseCase.NotifyViewDataChanged(_view.ID);
            };
            inheritSelectView.SetView(_view.ID);
            AddToolBarView(inheritSelectView);
        }
    }
}