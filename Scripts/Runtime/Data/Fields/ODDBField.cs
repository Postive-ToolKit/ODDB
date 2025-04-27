using System;
using TeamODD.ODDB.Runtime.Data.Enum;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data
{
    [Serializable]
    public class ODDBField
    {
        public ODDBID ID = new ODDBID();
        public string Name = string.Empty;
        public ODDBDataType Type = ODDBDataType.String;
        public ODDBField() {}
        public ODDBField(ODDBID id, string name, ODDBDataType type)
        {
            ID = id;
            Name = name;
            Type = type;
        }
    }
}