using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Enums;
using Object = UnityEngine.Object;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Attribute to mark fields that should bind to reference data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReferenceDataBindAttribute : System.Attribute
    {
        /// <summary>
        /// The type of reference data to bind to.
        /// </summary>
        public Type ReferenceType { get; }
        
        public ReferenceDataBindAttribute(Type referenceType)
        {
            if (referenceType.IsSubclassOf(typeof(Object)) == false)
                referenceType = typeof(Object);
            ReferenceType = referenceType;
        }
    }
    
    /// <summary>
    /// Extension methods for ReferenceDataBindAttribute.
    /// </summary>
    public static class ReferenceDataBindAttributeExtensions
    {
        private static readonly Dictionary<ODDBReferenceDataType, Type> _cache = new();
        
        /// <summary>
        /// Retrieves the reference data bind type associated with the given ODDBReferenceDataType enum value.
        /// </summary>
        /// <param name="referenceDataType"> The ODDBReferenceDataType enum value.</param>
        /// <returns> The associated reference data bind Type.</returns>
        public static Type GetReferenceDataBindType(this ODDBReferenceDataType referenceDataType)
        {
            if (_cache.ContainsKey(referenceDataType))
                return _cache[referenceDataType];
            
            var attr = referenceDataType
                .GetType()
                .GetField(referenceDataType.ToString())
                .GetCustomAttributes(typeof(ReferenceDataBindAttribute), false)
                .FirstOrDefault() as ReferenceDataBindAttribute;
            if (attr == null)
                _cache[referenceDataType] = typeof(Object);
            else
                _cache[referenceDataType] = attr.ReferenceType;
            return _cache[referenceDataType];
        }
    }
}