using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Params.Interfaces;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Attribute to specify a custom sub-data selector for a class or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class UseSubSelectorAttribute : Attribute
    {
        public string[] TypeKeys { get; }

        // v2.0 — preferred string-key constructor.
        public UseSubSelectorAttribute(params string[] typeKeys)
        {
            TypeKeys = typeKeys ?? Array.Empty<string>();
        }

        [Obsolete("Use string typeKey overload. Enum overload will be removed in T13.")]
        public UseSubSelectorAttribute(params ODDBDataType[] targetTypes)
        {
            TypeKeys = (targetTypes ?? Array.Empty<ODDBDataType>())
                .Select(t => t.ToWireKey())
                .ToArray();
        }

        // Back-compat shim for legacy callers that expected ODDBDataType[].
        [Obsolete("Use TypeKeys (string). Will be removed in T13.")]
        public ODDBDataType[] TargetTypes
        {
            get
            {
                var list = new List<ODDBDataType>();
                foreach (var key in TypeKeys)
                {
                    if (Enum.TryParse<ODDBDataType>(key, true, out var parsed))
                        list.Add(parsed);
                }
                return list.ToArray();
            }
        }
    }

    public static class UseSubSelectorAttributeExtensions
    {
        private static Dictionary<string, IFieldParamSelector> _cache;

        public static IFieldParamSelector GetTypeSubSelector(this ODDBDataType dataType)
        {
            return FindParamSelector(dataType.ToWireKey());
        }

        public static IFieldParamSelector FindParamSelector(string typeKey)
        {
            if (_cache == null)
                InitSubSelector();
            return _cache.TryGetValue(typeKey ?? string.Empty, out var sel) ? sel : null;
        }

        private static void InitSubSelector()
        {
            _cache = new Dictionary<string, IFieldParamSelector>();
            var drawerTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(type => typeof(IFieldParamSelector).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
            foreach (var drawerType in drawerTypes)
            {
                var attr = drawerType
                    .GetCustomAttributes(typeof(UseSubSelectorAttribute), false)
                    .FirstOrDefault() as UseSubSelectorAttribute;
                if (attr == null)
                    continue;
                if (Activator.CreateInstance(drawerType) is not IFieldParamSelector instance)
                    continue;
                foreach (var typeKey in attr.TypeKeys)
                {
                    if (string.IsNullOrEmpty(typeKey))
                        continue;
                    if (_cache.ContainsKey(typeKey))
                        continue;
                    _cache[typeKey] = instance;
                }
            }
        }
    }
}
