using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Editors.PropertyDrawers;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;

namespace TeamODD.ODDB.Editors.Attributes
{
    /// <summary>
    /// Attribute to specify a custom field drawer for a field in ODDB.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class CellDrawerAttribute : Attribute
    {
        public string TypeKey { get; }

        public CellDrawerAttribute(string typeKey)
        {
            TypeKey = typeKey;
        }

        [Obsolete("Use string typeKey overload")]
        public CellDrawerAttribute(ODDBDataType type)
        {
            TypeKey = type.ToWireKey();
        }
    }

    public static class ODDBCellDrawerAttributeExtensions
    {
        private static readonly Dictionary<ODDBDataType, IODDBCellDrawer> _cache = new();
        private static readonly Dictionary<string, IODDBCellDrawer> _customCache = new();

        public static IODDBCellDrawer GetCellDrawer(this ODDBDataType dataType, string param = "")
        {
            if (dataType == ODDBDataType.Custom)
            {
                if (string.IsNullOrEmpty(param))
                    return new StringCellDrawer();

                if (_customCache.Count == 0)
                    InitCustomCellDrawers();

                return _customCache.TryGetValue(param, out var customDrawer)
                    ? customDrawer
                    : new StringCellDrawer();
            }

            if (_cache.Count == 0)
                InitCellDrawer();
            return _cache.TryGetValue(dataType, out var drawer) ? drawer : new StringCellDrawer();
        }

        private static void InitCellDrawer()
        {
            var drawerTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IODDBCellDrawer).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
            foreach (var drawerType in drawerTypes)
            {
                var attr = drawerType
                    .GetCustomAttributes(typeof(CellDrawerAttribute), false)
                    .FirstOrDefault() as CellDrawerAttribute;
                if (attr == null)
                    continue;
                // Map string TypeKey back to ODDBDataType for legacy lookup.
                if (!TryMapKeyToEnum(attr.TypeKey, out var dataType))
                    continue;
                if (_cache.ContainsKey(dataType))
                    continue;
                if (Activator.CreateInstance(drawerType) is IODDBCellDrawer instance)
                    _cache[dataType] = instance;
            }
        }

        private static bool TryMapKeyToEnum(string typeKey, out ODDBDataType dataType)
        {
            foreach (ODDBDataType value in Enum.GetValues(typeof(ODDBDataType)))
            {
                if (value.ToWireKey() == typeKey)
                {
                    dataType = value;
                    return true;
                }
            }
            dataType = default;
            return false;
        }

        private static void InitCustomCellDrawers()
        {
            var drawerTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IODDBCellDrawer).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            // Pre-calculate Type to TypeID mapping from CustomDataTypeAttributes
            var typeToIdMap = new Dictionary<Type, string>();
            var serializerTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.GetCustomAttributes(typeof(CustomDataTypeAttribute), false).Any());

            foreach (var serializerType in serializerTypes)
            {
                var attr = serializerType.GetCustomAttribute<CustomDataTypeAttribute>();
                if (attr != null && attr.DataType != null)
                {
                    typeToIdMap[attr.DataType] = attr.TypeID;
                }
            }

            foreach (var drawerType in drawerTypes)
            {
                var attr = drawerType.GetCustomAttribute<CustomCellDrawerAttribute>();
                if (attr == null)
                    continue;

                string resolvedTypeID = attr.TypeID;

                // If TargetType is used, resolve TypeID from mapping
                if (attr.TargetType != null && typeToIdMap.TryGetValue(attr.TargetType, out var idFromType))
                {
                    resolvedTypeID = idFromType;
                }

                if (string.IsNullOrEmpty(resolvedTypeID) || _customCache.ContainsKey(resolvedTypeID))
                    continue;

                if (Activator.CreateInstance(drawerType) is IODDBCellDrawer instance)
                    _customCache[resolvedTypeID] = instance;
            }
        }
    }
}
