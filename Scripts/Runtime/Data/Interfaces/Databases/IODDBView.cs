namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBView : 
        IODDBHasUniqueKey, 
        IODDBHasName, 
        IODDBHasTableMeta, 
        IODDBHasBindType, 
        IODDBTableMetaHandler, 
        IODDBHasParentView,
        IODDBSerialize
    {
        
    }
}