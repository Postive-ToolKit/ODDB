using System.Collections.Generic;

namespace Plugins.ODDB.Scripts.Runtime.Data.DTO
{
    public struct ODDatabaseDTO
    {
        public List<ODDBTableDTO> Tables;
        
        public ODDatabaseDTO(List<ODDBTableDTO> tables)
        {
            Tables = tables;
        }
    }
}