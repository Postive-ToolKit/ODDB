using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDatabase : IODDBContainer<IODDBView>, IODDBIDProvider, IODDBDTOConvertable<ODDatabaseDTO>
    {
        IODDBView GetView(ODDBID id);

        void NotifyDataChanged(ODDBID id);
    }
}