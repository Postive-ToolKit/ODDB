using TeamODD.ODDB.Runtime.DTO;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IODDatabase : IDBContainer<IView>, IODDBIDProvider, IDTOConvertable<DatabaseDTO>
    {
        IView GetView(ODDBID id);

        void NotifyDataChanged(ODDBID id);
    }
}