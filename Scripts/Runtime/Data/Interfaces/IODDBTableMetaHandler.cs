namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBTableMetaHandler
    {
        void AddField(ODDBField field);
        void RemoveField(int index);
        void SwapTableMeta(int indexA, int indexB);
        bool IsScopedMeta(int index);
    }
}