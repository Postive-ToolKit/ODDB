using System;

namespace TeamODD.ODDB.Runtime.Types
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ODDBTypeAttribute : Attribute
    {
        public string Key { get; }
        public Type TargetType { get; }
        public string Folder { get; }
        public bool RequiresParam { get; }

        public ODDBTypeAttribute(string key, Type targetType = null, string folder = "Other", bool requiresParam = false)
        {
            Key = key;
            TargetType = targetType;
            Folder = folder;
            RequiresParam = requiresParam;
        }
    }
}
