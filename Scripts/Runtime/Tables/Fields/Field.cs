using System;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Runtime
{
    [Serializable]
    public class Field
    {
        public FieldType Type = new FieldType();
        public string Name = "Default Field";
        
        public Field() {}
        public Field(string name, FieldType type)
        {
            Name = name;
            Type = type;
        }
    }
}