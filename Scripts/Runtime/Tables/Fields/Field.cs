using System;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Runtime
{
    [Serializable]
    public class Field
    {
        public ODDBID ID = new ODDBID();
        public FieldType Type = new FieldType();
        public string Name = string.Empty;
        
        public Field() {}
        public Field(ODDBID id, string name, ODDBDataType type)
        {
            ID = id;
            Name = name;
            Type = type;
        }
    }
}