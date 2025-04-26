using TeamODD.ODDB.Editors.UI.Interfaces;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public abstract class ODDBMultiColumnEditor : MultiColumnListView, IODDBUpdateUI, IODDBHasView
    {
        public bool IsDirty { get; set; }
        public abstract void SetView(string viewKey);
    }
}