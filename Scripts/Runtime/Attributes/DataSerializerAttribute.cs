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
        private static readonly Dictionary<string, IDataSerializer> _customCache = new();

        public static IDataSerializer GetDataSerializer(this ODDBDataType type, string param = "")
        {
            if (type == ODDBDataType.Custom)
            {
                if (string.IsNullOrEmpty(param))
                    return new StringSerializer();
                
                if (_customCache.Count == 0)
                    InitCustomSerializers();
                
                return _customCache.TryGetValue(param, out var customSerializer) 
                    ? customSerializer 
                    : new StringSerializer();
            }

            if (_cache.TryGetValue(type, out var cachedAttr))
                return cachedAttr;
            
            var fieldInfo = type.GetType().GetField(type.ToString());
            if (fieldInfo == null)
                return new StringSerializer();

            var attr = fieldInfo
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

        private static void InitCustomSerializers()
        {
            var serializerTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IDataSerializer).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            foreach (var serializerType in serializerTypes)
            {
                var attr = serializerType
                    .GetCustomAttributes(typeof(CustomDataTypeAttribute), false)
                    .FirstOrDefault() as CustomDataTypeAttribute;

                if (attr == null)
                    continue;

                if (_customCache.ContainsKey(attr.TypeID))
                    continue;

                if (Activator.CreateInstance(serializerType) is IDataSerializer instance)
                    _customCache[attr.TypeID] = instance;
            }
        }
    }
}