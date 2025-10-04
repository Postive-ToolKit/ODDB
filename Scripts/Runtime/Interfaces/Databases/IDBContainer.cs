using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IDBContainer<T>
    {
        IReadOnlyList<T> GetAll();
    }
}