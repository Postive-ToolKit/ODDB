using System;
using System.Collections.Generic;
namespace TeamODD.ODDB.Runtime.DTO
{
    [Serializable]
    public class ViewDTO : DTOBase
    {
        public string Name;
        public string ID;
        public List<Field> TableMetas = new List<Field>();
        public string BindType;
        public string ParentView;
        
        public ViewDTO() { }
        public ViewDTO(string name, string id, List<Field> tableMetas, string bindType, string parentView)
        {
            Name = name;
            ID = id;
            TableMetas = tableMetas;
            BindType = bindType;
            ParentView = parentView;
        }
        
        public override string ToString()
        {
            return $"ViewDTO: {Name} ({ID}), BindType: {BindType}, ParentView: {ParentView}, Metas: {TableMetas.Count}";
        }
    }
}