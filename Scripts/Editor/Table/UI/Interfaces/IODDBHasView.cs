using Plugins.ODDB.Scripts.Runtime.Data;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Interfaces;

namespace TeamODD.ODDB.Editors.UI.Interfaces
{
    public interface IODDBHasView
    {
        public void SetView(IODDBView view);
    }
}