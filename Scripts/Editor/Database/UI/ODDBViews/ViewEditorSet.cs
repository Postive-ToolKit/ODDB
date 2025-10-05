using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enum;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    /// <summary>
    /// View model for the ODDBDataEditor
    /// </summary>
    public class ViewEditorSet : VisualElement, IHasView
    {
        public readonly ODDBViewType Type;
        private readonly IView _view;
        private readonly List<IHasView> _viewListeners = new();
        private readonly VisualElement _upperView;
        private readonly Toolbar _toolbar;
        private readonly VisualElement _contentView;
        private readonly IODDBEditorUseCase _editorUseCase;
        private ViewEditorSet(IView view, ODDBViewType type)
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            Type = type;
            _view = view;
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

            _upperView = BuildUpperView();
            base.Add(_upperView);
            
            _toolbar = BuildToolBox();
            base.Add(_toolbar);
            _contentView = BuildContentView();
            base.Add(_contentView);
        }
        
        private VisualElement BuildUpperView()
        {
            var upperView = new VisualElement();
            upperView.style.flexShrink = 1;
            upperView.style.flexDirection = FlexDirection.Column;
            upperView.style.paddingBottom = 0;
            upperView.style.paddingTop = 0;
            upperView.style.paddingLeft = 0;
            upperView.style.paddingRight = 0;
            upperView.style.marginBottom = 0;
            upperView.style.marginTop = 0;
            upperView.style.marginLeft = 0;
            upperView.style.marginRight = 0;
            return upperView;
        }

        private Toolbar BuildToolBox()
        {
            var toolBox = new Toolbar();
            toolBox = new Toolbar();
            toolBox.style.flexShrink = 1;
            return toolBox;
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
        private new void Add(VisualElement visualElement)
        {
            if (visualElement == null)
                return;
            RegisterListener(visualElement);
            _upperView.Add(visualElement);
        }
        
        private void AddToolBarView(ToolbarButton button)
        {
            if (button == null)
                return;
            RegisterListener(button);
            _toolbar.Add(button);
        }

        private void AddToolBarMenu(ToolbarMenu toolbarMenu)
        {
            if (toolbarMenu == null)
                return;
            RegisterListener(toolbarMenu);
            _toolbar.Add(toolbarMenu);
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
            var view = _editorUseCase.GetViewByKey(viewKey);
            if (view == null)
                return;
            _viewListeners.ForEach(v => v.SetView(viewKey));
        }
        
        public class Factory
        {
            private readonly IODDBEditorUseCase _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            public ViewEditorSet Create(string viewId)
            {
                var view = _editorUseCase.GetViewByKey(viewId);
                if (view == null)
                    return null;
                
                var type = _editorUseCase.GetViewTypeByKey(viewId);
                
                var editor = new ViewEditorSet(view, type);
                switch (type)
                {
                    case ODDBViewType.View:
                        return BuildODDBViewEditor(view, editor);
                    case ODDBViewType.Table:
                        return BuildODDBTableEditor(view, editor);
                    default:
                        return null;
                }
            }

            private ViewEditorSet BuildODDBViewEditor(IView view, ViewEditorSet editorSet)
            {
                var viewInfoView = new ViewInfoView();
                viewInfoView.OnViewNameChanged += name => _editorUseCase.SetViewName(view.ID, name);
                editorSet.Add(viewInfoView);
                
                var viewEditor = new ViewEditor();
                editorSet.AddContent(viewEditor);
                
                var createRow = new ToolbarButton();
                createRow.text = "Add Metadata";
                createRow.clicked += () =>
                {
                    view.AddField(new Field());
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                editorSet.AddToolBarView(createRow);
                
                var bindClassSelectView = new BindClassSelectView();
                bindClassSelectView.SetView(view.ID);
                
                bindClassSelectView.OnBindClassChanged += bindType =>
                {
                    view.BindType = bindType;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                editorSet.AddToolBarMenu(bindClassSelectView);
                
                var inheritSelectView = new ParentViewSelectView();
                inheritSelectView.OnParentViewChanged += parentView =>
                {
                    view.ParentView = parentView;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                inheritSelectView.SetView(view.ID);
                editorSet.AddToolBarView(inheritSelectView);
                
                return editorSet;
            }
            
            private ViewEditorSet BuildODDBTableEditor(IView view, ViewEditorSet editorSet)
            {
                var table = view as Table;
                
                var viewInfoView = new ViewInfoView();
                viewInfoView.OnViewNameChanged += name => _editorUseCase.SetViewName(view.ID, name);
                editorSet.Add(viewInfoView);
                
                var tableEditor = new TableEditor();
                editorSet.AddContent(tableEditor);

                
                var createRow = new ToolbarButton();
                createRow.text = "Create Row";
                createRow.clicked += () =>
                {
                    table!.AddRow();
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                
                editorSet.AddToolBarView(createRow);
                
                var bindClassSelectView = new BindClassSelectView();
                bindClassSelectView.SetView(view.ID);
                bindClassSelectView.OnBindClassChanged += bindType =>
                {
                    view.BindType = bindType;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                editorSet.AddToolBarMenu(bindClassSelectView);

                var inheritSelectView = new ParentViewSelectView();
                inheritSelectView.OnParentViewChanged += parentView =>
                {
                    view.ParentView = parentView;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                inheritSelectView.SetView(view.ID);
                editorSet.AddToolBarView(inheritSelectView);
                
                return editorSet;
            }
        }
    }
}