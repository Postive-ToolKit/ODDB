using TeamODD.ODDB.Scripts.Runtime.Data;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBTableMetaHandler
    {
        void AddField(ODDBTableMeta tableMeta);
        void RemoveTableMeta(int index);
        void SwapTableMeta(int indexA, int indexB);
    }
}