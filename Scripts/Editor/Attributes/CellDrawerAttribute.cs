using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors.PropertyDrawers;
using TeamODD.ODDB.Runtime.Enums;

namespace TeamODD.ODDB.Editors.Attributes
{
    /// <summary>
    /// Attribute to specify a custom field drawer for a field in ODDB
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class CellDrawerAttribute : Attribute
    {
        public ODDBDataType TargetType { get; set; } = ODDBDataType.String;
        
        public CellDrawerAttribute(ODDBDataType targetType)
        {
            TargetType = targetType;
        }
    }
    
    public static class ODDBCellDrawerAttributeExtensions
    {
        private static readonly Dictionary<ODDBDataType, IODDBCellDrawer> _cache = new();
        
        public static IODDBCellDrawer GetCellDrawer(this ODDBDataType dataType)
        {
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
                if(_cache.ContainsKey(attr.TargetType))
                    continue;
                if (Activator.CreateInstance(drawerType) is IODDBCellDrawer instance)
                    _cache[attr.TargetType] = instance;
            }
        }
    }
}