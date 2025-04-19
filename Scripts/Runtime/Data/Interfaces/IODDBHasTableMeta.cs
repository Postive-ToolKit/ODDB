using System.Collections.Generic;
using TeamODD.ODDB.Scripts.Runtime.Data;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBHasTableMeta
    {
        List<ODDBTableMeta> TableMetas { get; }
        List<ODDBTableMeta> ScopedTableMetas { get; }
    }
}