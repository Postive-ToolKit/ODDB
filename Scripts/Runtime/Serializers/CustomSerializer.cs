using TeamODD.ODDB.Runtime.Types;

namespace TeamODD.ODDB.Runtime.Serializers
{
    /// <summary>
    /// Fallback serializer for the "custom" type key.
    /// Concrete behaviour is delegated to user-registered [CustomDataType] serializers
    /// looked up via the legacy custom-cache; this implementation only exposes the
    /// "custom" key in the field-type dropdown.
    /// </summary>
    [ODDBType("custom", targetType: typeof(string), folder: "Custom", requiresParam: true)]
    public class CustomSerializer : IDataSerializer
    {
        public string Serialize(object data, string param)
        {
            return data?.ToString() ?? string.Empty;
        }

        public object Deserialize(string serializedData, string param)
        {
            return serializedData ?? string.Empty;
        }
    }
}
