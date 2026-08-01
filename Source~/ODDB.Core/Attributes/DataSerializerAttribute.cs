using System;
using TeamODD.ODDB.Runtime.Serializers;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Attribute to specify a serializer type. Retained for back-compat with any
    /// legacy field-level annotations; new code should rely on [ODDBType] and TypeRegistry.
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
}
