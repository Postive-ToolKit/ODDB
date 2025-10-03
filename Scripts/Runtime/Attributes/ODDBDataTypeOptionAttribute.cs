using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Data.Enum;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Attribute to provide metadata for ODDB fields, including field drawer type and visibility in type selector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class ODDBDataTypeOptionAttribute : Attribute
    {
        public bool IsHideInSelector { get; }
        
        public ODDBDataTypeOptionAttribute(bool isHideInSelector = false)
        {
            IsHideInSelector = isHideInSelector;
        }
    }
    
    public static class ODDBDataTypeOptionAttributeExtensions
    {
        private static readonly Dictionary<ODDBDataType, ODDBDataTypeOptionAttribute> _cache = new();
        public static ODDBDataTypeOptionAttribute GetDataTypeOption(this ODDBDataType enumValue)
        {
            if (_cache.TryGetValue(enumValue, out var cachedAttr))
                return cachedAttr;
            
            var attr = enumValue
                .GetType()
                .GetField(enumValue.ToString())
                .GetCustomAttributes(typeof(ODDBDataTypeOptionAttribute), false)
                .FirstOrDefault() as ODDBDataTypeOptionAttribute;
            _cache[enumValue] = attr ?? new ODDBDataTypeOptionAttribute();
            
            return _cache[enumValue];
        }
    }
}