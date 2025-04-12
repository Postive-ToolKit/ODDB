using System.Collections.Generic;
using TeamODD.ODDB.Scripts.Runtime.Data;

namespace Plugins.ODDB.Scripts.Runtime.Data.Interfaces
{
    public interface IODDBHasTableMeta
    {
        public List<ODDBTableMeta> TableMetas { get; }
    }
}