using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public abstract class ODDBMultiColumnEditor : MultiColumnListView, IODDBUpdateUI, IODDBHasView
    {
        public bool IsDirty { get; set; }
        public abstract void SetView(IODDBView view);
    }
}