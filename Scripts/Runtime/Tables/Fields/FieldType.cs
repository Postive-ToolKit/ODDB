using System;
using System.Text;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    [Serializable]
    public class FieldType
    {
        public const string PARAM_FIELD = nameof(Param);
        public const string TYPE_KEY_FIELD = nameof(_typeKey);

        public string Param = string.Empty;

        [SerializeField] private string _typeKey = "string";
        public string TypeKey
        {
            get => _typeKey ?? string.Empty;
            set => _typeKey = value ?? string.Empty;
        }

        public FieldType() {}

        public FieldType(string typeKey, string param = "")
        {
            _typeKey = typeKey ?? string.Empty;
            Param = param ?? string.Empty;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(TypeKey);
            if (!string.IsNullOrEmpty(Param))
            {
                sb.Append(" - ");
                sb.Append(Param);
            }
            return sb.ToString();
        }

        public static implicit operator FieldType(string typeKey)
        {
            return new FieldType(typeKey);
        }
    }
}
