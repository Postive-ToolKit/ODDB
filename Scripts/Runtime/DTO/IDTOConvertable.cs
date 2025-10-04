namespace TeamODD.ODDB.Runtime.DTO
{
    public interface IDTOConvertable<T> where T : DTOBase
    {
        T ToDTO();
        void FromDTO(T dto);
    }
}