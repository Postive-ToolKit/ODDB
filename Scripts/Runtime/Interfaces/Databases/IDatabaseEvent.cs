namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IDatabaseEvent
    {
        public void OnDatabaseInitialize(ODDatabase database);
        public void OnDatabaseDispose(ODDatabase database);
    }
}