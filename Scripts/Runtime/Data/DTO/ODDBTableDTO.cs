using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Interfaces;

namespace TeamODD.ODDB.Runtime.Data.DTO
{
    public class ODDBTableDTO : ODDBViewDTO
    {
        public string Data;
        public ODDBTableDTO() : base() { }
        public ODDBTableDTO(
            string name,
            string key,
            List<ODDBTableMeta> tableMetas,
            string bindType,
            string parentView,
            string data) : base(name, key, tableMetas, bindType, parentView)
        {
            Data = data;
        }
    }
}