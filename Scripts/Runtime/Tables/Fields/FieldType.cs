using System;
using System.Text;
using TeamODD.ODDB.Runtime.Enums;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    [Serializable]
    public class FieldType
    {
        public const string TYPE_FIELD = nameof(Type);
        public const string PARAM_FIELD = nameof(Param);
        public const string TYPE_KEY_FIELD = nameof(_typeKey);

        public ODDBDataType Type = ODDBDataType.String;
        public string Param = string.Empty;

        // v2.0 — preferred string key. Derived from Type enum when blank.
        [SerializeField] private string _typeKey = string.Empty;
        public string TypeKey
        {
            get => string.IsNullOrEmpty(_typeKey) ? Type.ToWireKey() : _typeKey;
            set => _typeKey = value;
        }

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
