using Plugins.ODDB.Scripts.Runtime.Data;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Runtime.Data;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public abstract class ODDBMultiColumnView : MultiColumnListView, IODDBUpdateUI, IODDBHasView
    {
        public bool IsDirty { get; set; }
        public abstract void SetView(ODDBView view);
        public virtual void UpdateMaxWidth(float maxWidth = -1){ }
    }
}