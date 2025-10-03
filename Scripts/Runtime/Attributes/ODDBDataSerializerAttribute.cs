using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Data.Enum;
using TeamODD.ODDB.Runtime.Data.Serializers;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Attribute to provide metadata for ODDB fields, including field drawer type and visibility in type selector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class ODDBDataSerializerAttribute : Attribute
    {
        public Type SerializerType { get; }
        
        public ODDBDataSerializerAttribute(Type serializerType)
        {
            if (typeof(IODDBDataSerializer).IsAssignableFrom(serializerType))
                SerializerType = serializerType;
            else
                SerializerType = typeof(ODDBStringSerializer);
        }
    }
    
    public static class ODDBDataSerializerAttributeExtensions
    {
        private static readonly Dictionary<ODDBDataType, IODDBDataSerializer> _cache = new();
        public static IODDBDataSerializer GetDataSerializer(this ODDBDataType type)
        {
            if (_cache.TryGetValue(type, out var cachedAttr))
                return cachedAttr;
            
            var attr = type
                .GetType()
                .GetField(type.ToString())
                .GetCustomAttributes(typeof(ODDBDataSerializerAttribute), false)
                .FirstOrDefault() as ODDBDataSerializerAttribute;

            if (attr == null)
            {
                _cache[type] = new ODDBStringSerializer();
            }
            else
            {
                _cache[type] = Activator.CreateInstance(attr.SerializerType) as IODDBDataSerializer 
                               ?? new ODDBStringSerializer();
            }
            return _cache[type];
        }
    }
}