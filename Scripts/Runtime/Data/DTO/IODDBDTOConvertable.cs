namespace TeamODD.ODDB.Runtime.Data.DTO
{
    public interface IODDBDTOConvertable<T> where T : ODDBDTO
    {
        T ToDTO();
        void FromDTO(T dto);
    }
}