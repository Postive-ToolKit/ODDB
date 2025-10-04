using System;
using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.DTO
{
    [Serializable]
    public class TableDTO : ViewDTO
    {
        public string[][] Data;
        public TableDTO() : base() { }
        public TableDTO(
            string name,
            string key,
            List<Field> tableMetas,
            string bindType,
            string parentView,
            string[][] data) : base(name, key, tableMetas, bindType, parentView)
        {
            Data = data;
        }
    }
}