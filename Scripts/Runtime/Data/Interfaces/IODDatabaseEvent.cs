namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDatabaseEvent
    {
        public void OnDatabaseInitialize(ODDatabase database);
        public void OnDatabaseDispose(ODDatabase database);
    }
}