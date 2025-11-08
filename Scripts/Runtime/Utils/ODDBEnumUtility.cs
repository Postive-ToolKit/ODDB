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
        private static Dictionary<string, Dictionary<string,Enum>> _enumValuesCache = new Dictionary<string, Dictionary<string,Enum>>();

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
        public static Dictionary<string,Enum> GetEnumValues(string enumName)
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