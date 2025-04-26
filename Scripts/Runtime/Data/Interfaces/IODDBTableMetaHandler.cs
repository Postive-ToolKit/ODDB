namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBTableMetaHandler
    {
        void AddField(ODDBTableMeta tableMeta);
        void RemoveField(int index);
        void SwapTableMeta(int indexA, int indexB);
        bool IsScopedMeta(int index);
    }
}