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

        public void SetView(string viewId)
        {
            _editorSet?.RemoveFromHierarchy();
            _editorSet = new ViewEditorSet(viewId);
            Add(_editorSet);
        }
    }
}
