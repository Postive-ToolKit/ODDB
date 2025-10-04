using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Enum;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Attribute to provide metadata for ODDB fields, including field drawer type and visibility in type selector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class DataTypeOptionAttribute : Attribute
    {
        public bool IsHideInSelector { get; }
        
        public DataTypeOptionAttribute(bool isHideInSelector = false)
        {
            IsHideInSelector = isHideInSelector;
        }
    }
    
    public static class ODDBDataTypeOptionAttributeExtensions
    {
        private static readonly Dictionary<ODDBDataType, DataTypeOptionAttribute> _cache = new();
        public static DataTypeOptionAttribute GetDataTypeOption(this ODDBDataType enumValue)
        {
            if (_cache.TryGetValue(enumValue, out var cachedAttr))
                return cachedAttr;
            
            var attr = enumValue
                .GetType()
                .GetField(enumValue.ToString())
                .GetCustomAttributes(typeof(DataTypeOptionAttribute), false)
                .FirstOrDefault() as DataTypeOptionAttribute;
            _cache[enumValue] = attr ?? new DataTypeOptionAttribute();
            
            return _cache[enumValue];
        }
    }
}