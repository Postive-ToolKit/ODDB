using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDatabase : IODDBContainer<IODDBView>, IODDBSerialize, IODDBIDProvider
    {
        IODDBView GetView(ODDBID id);

        void NotifyDataChanged(ODDBID id);
    }
}