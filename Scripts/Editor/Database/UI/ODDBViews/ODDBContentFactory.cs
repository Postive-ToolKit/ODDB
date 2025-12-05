using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public static class ODDBContentFactory
    {
        public static VisualElement CreateContent(ODDBViewType type, IView view, IODDBEditorUseCase useCase, Toolbar toolbar)
        {
            VisualElement content = null;
            switch (type)
            {
                case ODDBViewType.View:
                    content = new ViewEditor();
                    AddViewButtons(view, useCase, toolbar);
                    break;
                case ODDBViewType.Table:
                    content = new TableEditor();
                    AddTableButtons(view as Table, useCase, toolbar);
                    break;
            }
            
            // Register listeners if content needs view update
            if(content is IHasView hasView)
                hasView.SetView(view.ID);
                
            return content;
        }
        
        private static void AddViewButtons(IView view, IODDBEditorUseCase useCase, Toolbar toolbar)
        {
            // Add Field Button
            var createRow = new ToolbarButton { text = "Add Field" };
            createRow.clicked += () =>
            {
                view.AddField(new Field());
                useCase.NotifyViewDataChanged(view.ID);
            };
            AddToolbar(toolbar, createRow, view.ID);

            // Bind Class Select
            var bindClassSelectView = new BindClassSelectView();
            bindClassSelectView.SetView(view.ID);
            bindClassSelectView.OnBindClassChanged += bindType =>
            {
                view.BindType = bindType;
                useCase.NotifyViewDataChanged(view.ID);
            };
            AddToolbar(toolbar, bindClassSelectView, view.ID);

            // Parent Select
            var inheritSelectView = new ParentViewSelectView();
            inheritSelectView.OnParentViewChanged += parentView =>
            {
                view.ParentView = parentView;
                useCase.NotifyViewDataChanged(view.ID);
            };
            inheritSelectView.SetView(view.ID);
            AddToolbar(toolbar, inheritSelectView, view.ID);
        }

        private static void AddTableButtons(Table table, IODDBEditorUseCase useCase, Toolbar toolbar)
        {
            if (table == null) return;

            // Create Row Button
            var createRow = new ToolbarButton { text = "Create Row" };
            createRow.clicked += () =>
            {
                table.AddRow();
                useCase.NotifyViewDataChanged(table.ID);
            };
            AddToolbar(toolbar, createRow, table.ID);
            
            // Bind Class Select
            var bindClassSelectView = new BindClassSelectView();
            bindClassSelectView.SetView(table.ID);
            bindClassSelectView.OnBindClassChanged += bindType =>
            {
                table.BindType = bindType;
                useCase.NotifyViewDataChanged(table.ID);
            };
            AddToolbar(toolbar, bindClassSelectView, table.ID);

            // Parent Select
            var inheritSelectView = new ParentViewSelectView();
            inheritSelectView.OnParentViewChanged += parentView =>
            {
                table.ParentView = parentView;
                useCase.NotifyViewDataChanged(table.ID);
            };
            inheritSelectView.SetView(table.ID);
            AddToolbar(toolbar, inheritSelectView, table.ID);
        }

        private static void AddToolbar(Toolbar toolbar, VisualElement element, string viewId)
        {
             // Note: toolbar elements don't strictly need SetView if they are re-created every time.
             // But some custom views like BindClassSelectView might need it.
             // In Factory pattern, we create new instances, so SetView is called once here.
             // We don't need to register them to a listener list because they are destroyed when mode changes.
             if(element is IHasView hasView) hasView.SetView(viewId);
             toolbar.Add(element);
        }
    }
}