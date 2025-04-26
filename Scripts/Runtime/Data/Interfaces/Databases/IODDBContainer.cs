using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBContainer<T>
    {
        IReadOnlyList<T> GetAll();
    }
}