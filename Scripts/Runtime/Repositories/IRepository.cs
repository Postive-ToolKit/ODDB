using TeamODD.ODDB.Runtime.DTO;

namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IRepository<T> : IDBCRUDS<T>, IDBContainer<T>
    {
        public IODDBIDProvider KeyProvider { get; set; }
    }
}