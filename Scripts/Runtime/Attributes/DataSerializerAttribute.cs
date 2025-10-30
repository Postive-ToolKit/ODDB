using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Serializers;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Attribute to provide metadata for ODDB fields, including field drawer type and visibility in type selector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class DataSerializerAttribute : Attribute
    {
        public Type SerializerType { get; }
        
        public DataSerializerAttribute(Type serializerType)
        {
            if (typeof(IDataSerializer).IsAssignableFrom(serializerType))
                SerializerType = serializerType;
            else
                SerializerType = typeof(StringSerializer);
        }
    }
    
    public static class ODDBDataSerializerAttributeExtensions
    {
        private static readonly Dictionary<ODDBDataType, IDataSerializer> _cache = new();
        public static IDataSerializer GetDataSerializer(this ODDBDataType type)
        {
            if (_cache.TryGetValue(type, out var cachedAttr))
                return cachedAttr;
            
            var attr = type
                .GetType()
                .GetField(type.ToString())
                .GetCustomAttributes(typeof(DataSerializerAttribute), false)
                .FirstOrDefault() as DataSerializerAttribute;

            if (attr == null)
            {
                _cache[type] = new StringSerializer();
            }
            else
            {
                _cache[type] = Activator.CreateInstance(attr.SerializerType) as IDataSerializer 
                               ?? new StringSerializer();
            }
            return _cache[type];
        }
    }
}