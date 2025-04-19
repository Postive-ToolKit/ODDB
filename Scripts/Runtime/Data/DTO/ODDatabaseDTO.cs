using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Data.DTO
{
    public class ODDatabaseDTO
    {
        public List<ODDBTableDTO> Tables;
        public List<ODDBViewDTO> Views;
        
        public ODDatabaseDTO() { }
        public ODDatabaseDTO(List<ODDBTableDTO> tables, List<ODDBViewDTO> views)
        {
            Tables = tables;
            Views = views;
        }
    }
}