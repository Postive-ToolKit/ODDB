using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Runtime;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDBEditorView : VisualElement, IHasView
    {
        private View _view;
        private ViewEditorSet _editorSet;

        public ODDBEditorView()
        {
            style.flexGrow = 1;
            style.flexShrink = 0;
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Stretch; // FlexStart에서 Stretch로 변경
        }

        public void SetView(string viewKey)
        {
            _editorSet?.RemoveFromHierarchy();
            
            var factory = new ViewEditorSet.Factory();
            _editorSet = factory.Create(viewKey);
            Add(_editorSet);
        }
    }
}
