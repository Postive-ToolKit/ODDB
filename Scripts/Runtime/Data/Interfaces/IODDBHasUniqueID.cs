using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBHasUniqueID
    {
        public ODDBID ID { get; set; }
    }
}