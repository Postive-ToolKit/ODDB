using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDatabase : IODDBContainer<IODDBView>, IODDBSerialize, IODDBKeyProvider
    {
        IODDBView GetView(ODDBID id);
    }
}