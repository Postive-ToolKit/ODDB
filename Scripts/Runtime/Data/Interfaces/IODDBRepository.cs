using TeamODD.ODDB.Runtime.Data.Interfaces;

namespace TeamODD.ODDB.Runtime.Data
{
    public interface IODDBRepository<T> : IODDBCRUDS<T>, IODDBSerialize
    {
        
    }
}