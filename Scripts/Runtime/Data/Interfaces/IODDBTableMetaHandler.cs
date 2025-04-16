using TeamODD.ODDB.Scripts.Runtime.Data;

namespace Plugins.ODDB.Scripts.Runtime.Data.Interfaces
{
    public interface IODDBTableMetaHandler
    {
        void AddField(ODDBTableMeta tableMeta);
        void RemoveTableMeta(int index);
        void SwapTableMeta(int indexA, int indexB);
    }
}