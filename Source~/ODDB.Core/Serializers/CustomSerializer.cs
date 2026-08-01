using TeamODD.ODDB.Runtime.Types;

namespace TeamODD.ODDB.Runtime.Serializers
{
    /// <summary>
    /// Fallback serializer for the "custom" type key.
    /// </summary>
    [ODDBType("custom", targetType: typeof(string), folder: "Custom", requiresParam: true, flattenMenu: true)]
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
