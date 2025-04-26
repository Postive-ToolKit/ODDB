namespace TeamODD.ODDB.Runtime.Data.DTO
{
    public class ODDatabaseDTO
    {
        public string TableRepoData;
        public string ViewRepoData;
        public ODDatabaseDTO() { }
        public ODDatabaseDTO(string tableRepoData, string viewRepoData)
        {
            TableRepoData = tableRepoData;
            ViewRepoData = viewRepoData;
        }
    }
}