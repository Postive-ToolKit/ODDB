﻿using System;
using System.Collections.Generic;
namespace TeamODD.ODDB.Runtime.Data.DTO
{
    [Serializable]
    public class ODDBViewDTO
    {
        public string Name;
        public string ID;
        public List<ODDBField> TableMetas = new List<ODDBField>();
        public string BindType;
        public string ParentView;
        
        public ODDBViewDTO() { }
        public ODDBViewDTO(string name, string id, List<ODDBField> tableMetas, string bindType, string parentView)
        {
            Name = name;
            ID = id;
            TableMetas = tableMetas;
            BindType = bindType;
            ParentView = parentView;
        }
    }
}