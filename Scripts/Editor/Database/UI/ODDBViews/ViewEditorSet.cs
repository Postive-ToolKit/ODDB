using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    /// <summary>
    /// The main container for editing a specific View or Table.
    /// It coordinates the Header, Toolbar, and Content (Editor) components.
    /// </summary>
    public class ViewEditorSet : VisualElement, IHasView
    {
        private readonly List<IHasView> _viewListeners = new();
        private readonly Toolbar _editorToolBar;
        private readonly VisualElement _contentView;
        private readonly IODDBEditorUseCase _editorUseCase;
        private readonly ODDBHeaderView _headerView;
        
        private IView _view;
        private ODDBViewType _type;
        
        public ViewEditorSet(string viewId)
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1;
            style.flexShrink = 1;
            style.alignContent = Align.FlexStart;
            style.paddingBottom = 0;
            style.paddingTop = 0;
            style.paddingLeft = 0;
            style.paddingRight = 0;
            style.marginBottom = 0;
            style.marginTop = 0;
            style.marginLeft = 0;
            style.marginRight = 0;

            // 1. Header View (Name, ID, Type)
            _headerView = new ODDBHeaderView(_editorUseCase);
            _headerView.OnTypeChanged += (newType) => SetMode(newType);
            Add(_headerView);

            // 2. Editor Toolbar (Add Field/Row, Bind, Parent)
            _editorToolBar = new Toolbar { style = { flexShrink = 1 } };
            Add(_editorToolBar);

            // 3. Content View (Table/View Editor)
            _contentView = new VisualElement { style = { flexGrow = 1 } };
            Add(_contentView);
            
            SetView(viewId);
        }

        /// <summary>
        /// Updates the view to display the specified view/table ID.
        /// </summary>
        public void SetView(string viewKey)
        {
            if (string.IsNullOrEmpty(viewKey))
            {
                ClearView();
                return;
            }

            if (_view != null && _view.ID == viewKey)
                return;
            
            _view = _editorUseCase.GetViewByKey(viewKey);
            if (_view == null)
            {
                ClearView();
                return;
            }

            _type = _editorUseCase.GetViewTypeByKey(_view.ID);
            
            // Update Header
            _headerView.UpdateView(_view, _type);
            
            // Update Content & Mode
            SetMode(_type);
            
            // Update Listeners
            foreach (var listener in _viewListeners)
                listener.SetView(viewKey);
        }

        private void ClearView()
        {
            _view = null;
            _headerView.ClearView();
            _editorToolBar.Clear();
            _contentView.Clear();
        }

        /// <summary>
        /// Switches the editor mode (View or Table) and rebuilds the content.
        /// </summary>
        private void SetMode(ODDBViewType type)
        {
            _contentView.Clear();
            _editorToolBar.Clear();
            _viewListeners.Clear();

            // Use Factory to create content and populate toolbar
            var content = ODDBContentFactory.CreateContent(type, _view, _editorUseCase, _editorToolBar);
            if (content != null)
            {
                content.style.flexGrow = 1;
                content.style.flexShrink = 1;
                _contentView.Add(content);
                if (content is IHasView hasView)
                {
                    _viewListeners.Add(hasView);
                }
            }
        }
    }
}