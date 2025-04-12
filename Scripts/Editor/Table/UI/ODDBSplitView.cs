using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
#if UNITY_2022_2_OR_NEWER
    [UxmlElement]
    public partial class ODDBSplitView : TwoPaneSplitView
#else
    public class ODDBSplitView : TwoPaneSplitView
#endif
    {
#if !UNITY_2022_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<ODDBSplitView, TwoPaneSplitView.UxmlTraits> { }
#endif
        public ODDBSplitView() 
        {
            style.flexGrow = 1;
        }
    }
}