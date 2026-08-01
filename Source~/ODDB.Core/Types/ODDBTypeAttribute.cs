using System;
using TeamODD.ODDB.Runtime.Enums;

namespace TeamODD.ODDB.Runtime.Types
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ODDBTypeAttribute : Attribute
    {
        public string Key { get; }
        public Type TargetType { get; }
        public string Folder { get; }
        public bool RequiresParam { get; }

        /// <summary>
        /// When true, the FieldType dropdown skips this type's own menu node and
        /// promotes its Param sub-selector options directly into the parent folder.
        /// Useful for "meta" type keys (e.g. <c>"custom"</c>) whose only role is to
        /// gate a Param picker — without this flag the menu shows a redundant
        /// <c>Folder &gt; Key &gt; Param</c> chain where the middle level adds no info.
        /// Implies <see cref="RequiresParam"/> for the flatten to be meaningful.
        /// </summary>
        public bool FlattenMenu { get; }
        public ODDBLoadType LoadType { get; }

        public ODDBTypeAttribute(string key, Type targetType = null, string folder = "Other", bool requiresParam = false, bool flattenMenu = false)
            : this(key, ODDBLoadType.Default, targetType, folder, requiresParam, flattenMenu)
        {
        }

        public ODDBTypeAttribute(string key, ODDBLoadType loadType, Type targetType = null, string folder = "Other", bool requiresParam = false, bool flattenMenu = false)
        {
            Key = key;
            LoadType = loadType;
            TargetType = targetType;
            Folder = folder;
            RequiresParam = requiresParam;
            FlattenMenu = flattenMenu;
        }
    }
}
