using System;
using System.Text;
using TeamODD.ODDB.Runtime.Enum;

namespace TeamODD.ODDB.Runtime
{
    [Serializable]
    public class FieldType
    {
        public const string TYPE_FIELD = nameof(Type);
        public const string PARAM_FIELD = nameof(Param);
        
        public ODDBDataType Type = ODDBDataType.String;
        public string Param = string.Empty;
        
        public FieldType() {}

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Type.ToString());
            if (!string.IsNullOrEmpty(Param))
            {
                sb.Append(" - ");
                sb.Append(Param);
            }
            return sb.ToString();
        }

        public static implicit operator ODDBDataType(FieldType fieldType)
        {
            return fieldType.Type;
        }
        
        public static implicit operator FieldType(ODDBDataType dataType)
        {
            return new FieldType { Type = dataType };
        }
    }
}