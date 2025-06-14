namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBView : 
        IODDBHasUniqueID, 
        IODDBHasName, 
        IODDBHasTableMeta, 
        IODDBHasBindType, 
        IODDBTableMetaHandler, 
        IODDBHasParentView,
        IODDBSerialize
    {
        
    }
}