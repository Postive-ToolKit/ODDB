using TeamODD.ODDB.Runtime.DTO;

namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IView : 
        IHasODDBID, 
        IHasName, 
        IHasFields, 
        IHasBindType, 
        IFieldsHandler, 
        IHasParentView,
        IDTOConvertable<ViewDTO>
    {
        
    }
}