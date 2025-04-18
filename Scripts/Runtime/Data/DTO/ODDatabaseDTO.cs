using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data;

namespace TeamODD.ODDB.Runtime.Data.DTO
{
    public struct ODDatabaseDTO
    {
        public List<ODDBTableDTO> Tables;
        public List<ODDBViewDTO> Views;
        
        public ODDatabaseDTO(List<ODDBTableDTO> tables, List<ODDBViewDTO> views)
        {
            Tables = tables;
            Views = views;
        }

    }
}