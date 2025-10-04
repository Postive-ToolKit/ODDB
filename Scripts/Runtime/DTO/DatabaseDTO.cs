using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.DTO
{
    public class DatabaseDTO : DTOBase
    {
        public List<TableDTO> TableRepoData = new List<TableDTO>();
        public List<ViewDTO> ViewRepoData = new List<ViewDTO>();
        public DatabaseDTO() { }
        public DatabaseDTO(List<TableDTO> tableRepoData, List<ViewDTO> viewRepoData)
        {
            TableRepoData = tableRepoData;
            ViewRepoData = viewRepoData;
        }
    }
}