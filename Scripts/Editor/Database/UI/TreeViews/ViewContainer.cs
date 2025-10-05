using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.UI
{
    public class ViewContainer
    {
        public IView View;
        public List<ViewContainer> Children = new List<ViewContainer>();
    }
}