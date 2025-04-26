namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBRepository<T> : IODDBCRUDS<T>, IODDBContainer<T>, IODDBSerialize
    {
        public IODDBIDProvider KeyProvider { get; set; }
    }
}