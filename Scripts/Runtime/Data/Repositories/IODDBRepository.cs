namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBRepository<T> : IODDBCRUDS<T>, IODDBContainer<T>, IODDBSerialize
    {
        public IODDBKeyProvider KeyProvider { get; set; }
    }
}