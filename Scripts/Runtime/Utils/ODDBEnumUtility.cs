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
        private static Dictionary<string, Dictionary<int,Enum>> _enumValuesCache = new Dictionary<string, Dictionary<int,Enum>>();

        /// <summary>
        /// Initialize the ODDB Enum Types by scanning assemblies for enums with the ODDBEnumAttribute
        /// </summary>
        private static void Initialize()
        {
            if (_oddbEnumTypes.Count > 0)
                return;

            var targetEnums = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsEnum && type.GetCustomAttribute<ODDBEnumAttribute>() != null);

            foreach (var enumType in targetEnums)
            {
                if (_oddbEnumTypes.Contains(enumType))
                    continue;
                _oddbEnumTypes.Add(enumType);
                _enumTypeCache.Add(enumType.Name, enumType);
                _enumValuesCache.Add(enumType.Name, new Dictionary<int, Enum>());
                foreach (var value in Enum.GetValues(enumType))
                    _enumValuesCache[enumType.Name].Add((int)value, (Enum)value);
            }
        }

        /// <summary>
        /// Get Enum Type by its name
        /// </summary>
        /// <param name="enumName"> name of the Enum </param>
        /// <returns> Type or null </returns>
        public static Type GetEnumType(string enumName)
        {
            Initialize();
            return _enumTypeCache.GetValueOrDefault(enumName);
        }
        
        /// <summary>
        /// Get Enum Values by Enum Name
        /// </summary>
        /// <param name="enumName"> name of the Enum </param>
        /// <returns> Dictionary of int and Enum </returns>
        public static Dictionary<int,Enum> GetEnumValues(string enumName)
        {
            Initialize();
            return _enumValuesCache.GetValueOrDefault(enumName);
        }
        
        /// <summary>
        /// Get all ODDB Enum Types
        /// </summary>
        /// <returns> IEnumerable of Types </returns>
        public static IEnumerable<Type> GetAllOddbEnumTypes()
        {
            Initialize();
            return _oddbEnumTypes;
        }
    }
}