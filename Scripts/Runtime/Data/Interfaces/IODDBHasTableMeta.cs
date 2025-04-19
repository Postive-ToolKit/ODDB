using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBHasTableMeta
    {
        List<ODDBTableMeta> TableMetas { get; }
        List<ODDBTableMeta> ScopedTableMetas { get; }
    }
}