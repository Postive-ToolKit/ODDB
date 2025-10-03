using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Data.DTO
{
    public class ODDatabaseDTO : ODDBDTO
    {
        public List<ODDBTableDTO> TableRepoData = new List<ODDBTableDTO>();
        public List<ODDBViewDTO> ViewRepoData = new List<ODDBViewDTO>();
        public ODDatabaseDTO() { }
        public ODDatabaseDTO(List<ODDBTableDTO> tableRepoData, List<ODDBViewDTO> viewRepoData)
        {
            TableRepoData = tableRepoData;
            ViewRepoData = viewRepoData;
        }
    }
}