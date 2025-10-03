using TeamODD.ODDB.Runtime.Data.DTO;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBRepository<T> : IODDBCRUDS<T>, IODDBContainer<T>
    {
        public IODDBIDProvider KeyProvider { get; set; }
    }
}