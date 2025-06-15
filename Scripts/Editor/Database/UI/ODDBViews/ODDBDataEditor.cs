using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Enum;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    /// <summary>
    /// View model for the ODDBDataEditor
    /// </summary>
    public class ODDBDataEditor : VisualElement, IODDBHasView
    {
        public readonly ODDBViewType Type;
        private readonly IODDBView _view;
        private readonly List<IODDBHasView> _viewListeners = new();
        private readonly List<IODDBGeometryUpdate> _viewGeometryListeners = new();
        private readonly VisualElement _upperView;
        private readonly GroupBox _toolBox;
        private readonly ScrollView _contentView;
        private readonly IODDBEditorUseCase _editorUseCase;
        private ODDBDataEditor(IODDBView view, ODDBViewType type)
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
            
            _toolBox = BuildToolBox() as GroupBox;
            base.Add(_toolBox);
            _contentView = BuildContentView() as ScrollView;
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

        private VisualElement BuildToolBox()
        {
            var toolBox = new GroupBox();
            toolBox = new GroupBox();
            toolBox.style.flexShrink = 1;
            toolBox.style.flexDirection = FlexDirection.Row;
            toolBox.style.paddingBottom = 0;
            toolBox.style.paddingTop = 0;
            toolBox.style.paddingLeft = 0;
            toolBox.style.paddingRight = 0;
            toolBox.style.marginBottom = 0;
            toolBox.style.marginTop = 0;
            toolBox.style.marginLeft = 0;
            toolBox.style.marginRight = 0;
            return toolBox;
        }
        private VisualElement BuildContentView()
        {
            var contentView = new ScrollView();
            contentView.mode = ScrollViewMode.VerticalAndHorizontal;
            contentView.RegisterCallback<GeometryChangedEvent> (evt =>
            {
                foreach (var listener in _viewGeometryListeners) {
                    listener.UpdateGeometry(evt);
                }
            });
            return contentView;
        }
        private new void Add(VisualElement visualElement)
        {
            if (visualElement == null)
                return;
            RegisterListener(visualElement);
            _upperView.Add(visualElement);
        }
        
        private void AddToolBoxView(VisualElement visualElement)
        {
            if (visualElement == null)
                return;
            RegisterListener(visualElement);
            _toolBox.Add(visualElement);
        }
        private void AddContent(VisualElement visualElement)
        {
            if (visualElement == null)
                return;
            RegisterListener(visualElement);
            _contentView.Add(visualElement);
            
            if (visualElement is IODDBGeometryUpdate geometryUpdate)
                _viewGeometryListeners.Add(geometryUpdate);
        }
        private void RegisterListener(object listener)
        {
            if (listener is not IODDBHasView view)
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
            public ODDBDataEditor Create(string viewId)
            {
                var view = _editorUseCase.GetViewByKey(viewId);
                if (view == null)
                    return null;
                
                var type = _editorUseCase.GetViewTypeByKey(viewId);
                
                var editor = new ODDBDataEditor(view, type);
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

            private ODDBDataEditor BuildODDBViewEditor(IODDBView view, ODDBDataEditor editor)
            {
                var viewInfoView = new ODDBViewInfoView();
                viewInfoView.OnViewNameChanged += name => _editorUseCase.SetViewName(view.ID, name);
                editor.Add(viewInfoView);
                
                var viewEditor = new ODDBViewEditor();
                editor.AddContent(viewEditor);
                
                var createRow = new Button();
                createRow.text = "Add Metadata";
                createRow.clicked += () =>
                {
                    view.AddField(new ODDBField());
                    viewEditor.IsDirty = true;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                createRow.style.flexGrow = 0;
                createRow.style.flexShrink = 1;
                editor.AddToolBoxView(createRow);
                
                var bindClassSelectView = new ODDBBindClassSelectView();
                bindClassSelectView.SetView(view.ID);
                
                bindClassSelectView.OnBindClassChanged += bindType =>
                {
                    view.BindType = bindType;
                    viewEditor.IsDirty = true;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                editor.AddToolBoxView(bindClassSelectView);
                
                var inheritSelectView = new ODDBInheritSelectView();
                inheritSelectView.OnParentViewChanged += parentView =>
                {
                    view.ParentView = parentView;
                    viewEditor.IsDirty = true;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                inheritSelectView.SetView(view.ID);
                editor.AddToolBoxView(inheritSelectView);
                
                return editor;
            }
            
            private ODDBDataEditor BuildODDBTableEditor(IODDBView view, ODDBDataEditor editor)
            {
                var table = view as ODDBTable;
                
                var viewInfoView = new ODDBViewInfoView();
                viewInfoView.OnViewNameChanged += name => _editorUseCase.SetViewName(view.ID, name);
                editor.Add(viewInfoView);
                
                var tableEditor = new ODDBTableEditor();
                editor.AddContent(tableEditor);

                
                var createRow = new Button();
                createRow.text = "Create Row";
                createRow.clicked += () =>
                {
                    table!.AddRow();
                    tableEditor.IsDirty = true;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                createRow.style.flexGrow = 0;
                createRow.style.flexShrink = 1;
                editor.AddToolBoxView(createRow);
                
                // var autoWidth = new Toggle();
                // autoWidth.value = true;
                // autoWidth.text = "Auto Width";
                // autoWidth.style.flexGrow = 0;
                // autoWidth.style.flexShrink = 1;
                // autoWidth.RegisterCallback<ChangeEvent<bool>>(OnAutoWidthToggleChanged);
                // editor.AddToolBoxView(autoWidth);
                
                var bindClassSelectView = new ODDBBindClassSelectView();
                bindClassSelectView.SetView(view.ID);
                bindClassSelectView.OnBindClassChanged += bindType =>
                {
                    view.BindType = bindType;
                    tableEditor.IsDirty = true;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                editor.AddToolBoxView(bindClassSelectView);

                var inheritSelectView = new ODDBInheritSelectView();
                inheritSelectView.OnParentViewChanged += parentView =>
                {
                    view.ParentView = parentView;
                    tableEditor.IsDirty = true;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                inheritSelectView.SetView(view.ID);
                editor.AddToolBoxView(inheritSelectView);
                
                var exportButton = new Button();
                exportButton.text = "Export";
                exportButton.style.flexGrow = 0;
                exportButton.style.flexShrink = 1;
                exportButton.clicked += () =>
                {
                    var path = EditorUtility.SaveFilePanel("Export Table", "", table.Name + ".csv", "csv");
                    if (string.IsNullOrEmpty(path))
                        return;
                    var data = table.Serialize();
                    var utf8WithBom = new UTF8Encoding(true);
                    File.WriteAllText(path, data, utf8WithBom);
                };
                editor.AddToolBoxView(exportButton);
                
                var importButton = new Button();
                importButton.text = "Import";
                importButton.style.flexGrow = 0;
                importButton.style.flexShrink = 1;
                importButton.clicked += () =>
                {
                    var path = EditorUtility.OpenFilePanel("Import Table", "", "csv");
                    if (string.IsNullOrEmpty(path))
                        return;
                    var utf8WithBom = new UTF8Encoding(true);
                    var data = File.ReadAllText(path, utf8WithBom);
                    table!.Deserialize(data);
                    tableEditor.IsDirty = true;
                    _editorUseCase.NotifyViewDataChanged(view.ID);
                };
                editor.AddToolBoxView(importButton);
                
                return editor;
            }
        }
    }
}