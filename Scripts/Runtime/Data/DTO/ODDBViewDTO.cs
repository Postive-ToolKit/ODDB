using System.Collections.Generic;
namespace TeamODD.ODDB.Runtime.Data.DTO
{
    public class ODDBViewDTO
    {
        public string Name;
        public string Key;
        public List<ODDBTableMeta> TableMetas;
        public string BindType;
        public string ParentView;
        
        public ODDBViewDTO() { }
        public ODDBViewDTO(string name, string key, List<ODDBTableMeta> tableMetas, string bindType, string parentView)
        {
            Name = name;
            Key = key;
            TableMetas = tableMetas;
            BindType = bindType;
            ParentView = parentView;
        }


    }
}