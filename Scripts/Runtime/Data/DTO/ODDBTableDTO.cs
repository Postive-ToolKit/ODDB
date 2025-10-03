using System;
using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Data.DTO
{
    [Serializable]
    public class ODDBTableDTO : ODDBViewDTO
    {
        public string[][] Data;
        public ODDBTableDTO() : base() { }
        public ODDBTableDTO(
            string name,
            string key,
            List<ODDBField> tableMetas,
            string bindType,
            string parentView,
            string[][] data) : base(name, key, tableMetas, bindType, parentView)
        {
            Data = data;
        }
    }
}