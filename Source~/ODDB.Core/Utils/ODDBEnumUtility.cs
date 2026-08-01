using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Runtime.Attributes;

namespace TeamODD.ODDB.Runtime.Utils.Converters
{
    public static class ODDBEnumUtility
    {
        private static HashSet<Type> _oddbEnumTypes = new HashSet<Type>();
        private static Dictionary<string, Type> _enumTypeCache = new Dictionary<string, Type>();
        private static Dictionary<string, Dictionary<string, Enum>> _enumValuesCache = new Dictionary<string, Dictionary<string, Enum>>();

        internal static void ResetCache()
        {
            _oddbEnumTypes.Clear();
            _enumTypeCache.Clear();
            _enumValuesCache.Clear();
        }

        /// <summary>
        /// Initialize the ODDB Enum Types by scanning assemblies for enums with the ODDBEnumAttribute
        /// </summary>
        private static void Initialize()
        {
            if (_oddbEnumTypes.Count > 0)
                return;

            var targetEnums = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch (Exception exception) { return GetLoadableTypes(exception); }
                })
                .Where(type => type.IsEnum && type.GetCustomAttribute<ODDBEnumAttribute>() != null);

            foreach (var enumType in targetEnums)
            {
                if (_oddbEnumTypes.Contains(enumType))
                    continue;
                _oddbEnumTypes.Add(enumType);
                _enumTypeCache.Add(enumType.Name, enumType);
                _enumValuesCache.Add(enumType.Name, new Dictionary<string, Enum>());
                var enumList = Enum.GetValues(enumType).Cast<Enum>().ToList();
                _enumValuesCache[enumType.Name].Add(string.Empty, enumList.First());
                foreach (var value in enumList)
                    _enumValuesCache[enumType.Name].Add(value.ToString(), value);
            }
        }

        /// <summary>
        /// Get Enum Type by its name
        /// </summary>
        public static Type GetEnumType(string enumName)
        {
            Initialize();
            return _enumTypeCache.GetValueOrDefault(enumName);
        }

        /// <summary>
        /// Get Enum Values by Enum Name
        /// </summary>
        public static Dictionary<string, Enum> GetEnumValues(string enumName)
        {
            Initialize();
            return _enumValuesCache.GetValueOrDefault(enumName);
        }

        /// <summary>
        /// Get all ODDB Enum Types
        /// </summary>
        public static IEnumerable<Type> GetAllOddbEnumTypes()
        {
            Initialize();
            return _oddbEnumTypes;
        }

        private static Type[] GetLoadableTypes(Exception exception)
        {
            if (exception is ReflectionTypeLoadException typeLoadException && typeLoadException.Types != null)
            {
                var loadableTypes = new List<Type>();
                foreach (var type in typeLoadException.Types)
                {
                    if (type != null)
                        loadableTypes.Add(type);
                }
                return loadableTypes.ToArray();
            }

            return Array.Empty<Type>();
        }
    }
}
