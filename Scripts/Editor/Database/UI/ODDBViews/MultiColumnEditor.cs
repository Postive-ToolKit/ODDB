using TeamODD.ODDB.Editors.UI.Interfaces;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public abstract class MultiColumnEditor : MultiColumnListView, IHasView
    {
        public abstract void SetView(string viewKey);
    }
}