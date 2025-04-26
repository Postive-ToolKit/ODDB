using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBKeyProvider
    {
        ODDBID CreateKey();
    }
}