using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBHasTableMeta
    {
        List<ODDBField> TotalFields { get; }
        List<ODDBField> ScopedFields { get; }
    }
}