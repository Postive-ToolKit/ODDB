using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IHasFields
    {
        List<Field> TotalFields { get; }
        List<Field> ScopedFields { get; }
    }
}