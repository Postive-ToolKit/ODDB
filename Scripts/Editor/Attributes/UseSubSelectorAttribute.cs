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
        public ODDBDataType[] TargetTypes { get; set; }
        
        public UseSubSelectorAttribute(params ODDBDataType[] targetTypes)
        {
            TargetTypes = targetTypes;
        }
    }
    
    public static class UseSubSelectorAttributeExtensions
    {
        private static readonly Dictionary<ODDBDataType, IFieldParamSelector> _cache = new();
        
        public static IFieldParamSelector GetTypeSubSelector(this ODDBDataType dataType)
        {
            if (_cache.Count == 0)
                InitSubSelector();
            return _cache.TryGetValue(dataType, out var drawer) ? drawer : null;
        }

        private static void InitSubSelector()
        {
            var drawerTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
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
                foreach (var targetType in attr.TargetTypes)
                {
                    if(_cache.ContainsKey(targetType))
                        continue;
                    _cache[targetType] = instance;
                }
            }
        }
    }
}