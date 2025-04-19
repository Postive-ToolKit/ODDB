using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Runtime.Data;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDBEditorView : VisualElement, IODDBHasView
    {
        private ODDBView _view;
        private ODDBDataEditor _editor;

        public ODDBEditorView()
        {
            style.flexGrow = 1;
            style.flexShrink = 0;
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Stretch; // FlexStart에서 Stretch로 변경
        }

        public void SetView(string viewKey)
        {
            _editor?.RemoveFromHierarchy();
            
            var factory = new ODDBDataEditor.Factory();
            _editor = factory.Create(viewKey);
            Add(_editor);
        }
    }
}
