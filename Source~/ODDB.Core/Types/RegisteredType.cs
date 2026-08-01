using System;
using TeamODD.ODDB.Runtime.Serializers;

namespace TeamODD.ODDB.Runtime.Types
{
    public class RegisteredType
    {
        public string Key { get; }
        public Type TargetType { get; }
        public string Folder { get; }
        public bool RequiresParam { get; }
        public bool FlattenMenu { get; }
        public IDataSerializer Serializer { get; }

        public RegisteredType(string key, Type targetType, string folder, bool requiresParam, IDataSerializer serializer, bool flattenMenu = false)
        {
            Key = key;
            TargetType = targetType;
            Folder = folder;
            RequiresParam = requiresParam;
            FlattenMenu = flattenMenu;
            Serializer = serializer;
        }
    }
}
