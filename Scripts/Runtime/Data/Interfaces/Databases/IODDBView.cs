using TeamODD.ODDB.Runtime.Data.DTO;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBView : 
        IODDBHasUniqueID, 
        IODDBHasName, 
        IODDBHasTableMeta, 
        IODDBHasBindType, 
        IODDBTableMetaHandler, 
        IODDBHasParentView,
        IODDBDTOConvertable<ODDBViewDTO>
    {
        
    }
}