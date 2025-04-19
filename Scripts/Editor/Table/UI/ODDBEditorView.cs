using System;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Scripts.Runtime.Data;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    [UxmlElement]
    public partial class ODDBEditorView : VisualElement, IODDBHasView
    {
        public event Action<IODDBView> OnViewDataChanged; 
        private ODDBView _view;
        private ODDBDataEditor _editor;
        public ODDBEditorView()
        {
            style.flexGrow = 1;
            style.flexShrink = 0;
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Stretch; // FlexStart에서 Stretch로 변경
            
        }

        public void SetView(IODDBView view)
        {
            _editor?.RemoveFromHierarchy();
            var type = ODDBViewType.None;
            if (view is ODDBTable table) {
                type = ODDBViewType.Table;
            }
            else if (view is ODDBView) {
                type = ODDBViewType.View;
            }
            else {
                throw new ArgumentException("Invalid view type");
            }
            var factory = new ODDBDataEditor.Factory();
            _editor = factory.Create(view, type);
            _editor.OnViewDataChanged += OnViewDataChanged;
            Add(_editor);
        }
    }
}
