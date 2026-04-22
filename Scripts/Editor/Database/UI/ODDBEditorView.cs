using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Runtime;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDBEditorView : VisualElement, IHasView
    {
        private ViewEditorSet _editorSet;
        private Label _emptyStateLabel;

        public ODDBEditorView()
        {
            style.flexGrow = 1;
            style.flexShrink = 0;
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Stretch; // FlexStart에서 Stretch로 변경
            
            _emptyStateLabel = new Label("Select a Table or View from the left panel to start editing.");
            _emptyStateLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            _emptyStateLabel.style.flexGrow = 1;
            _emptyStateLabel.style.color = new UnityEngine.Color(0.6f, 0.6f, 0.6f);
            _emptyStateLabel.style.fontSize = 14;
            Add(_emptyStateLabel);
        }

        public void SetView(string viewId)
        {
            _editorSet?.RemoveFromHierarchy();
            
            if (string.IsNullOrEmpty(viewId))
            {
                _emptyStateLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                _emptyStateLabel.style.display = DisplayStyle.None;
                _editorSet = new ViewEditorSet(viewId);
                Add(_editorSet);
            }
        }
    }
}
