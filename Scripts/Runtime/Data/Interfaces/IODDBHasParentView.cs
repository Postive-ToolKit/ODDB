using Plugins.ODDB.Scripts.Runtime.Data;
using TeamODD.ODDB.Runtime.Data;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    /// <summary>
    /// Interface to represent an object that has a parent view.
    /// </summary>
    public interface IODDBHasParentView
    {
        IODDBView ParentView { get; set; }
    }
}