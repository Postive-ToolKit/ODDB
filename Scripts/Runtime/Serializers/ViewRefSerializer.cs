using TeamODD.ODDB.Runtime.Types;

namespace TeamODD.ODDB.Runtime.Serializers
{
    /// <summary>
    /// Serializer for View-reference data type.
    /// The serialized payload is the target row's ODDBID as a string;
    /// Param holds the referenced view's ID.
    /// </summary>
    [ODDBType("view", targetType: typeof(string), folder: "References", requiresParam: true)]
    public class ViewRefSerializer : IDataSerializer
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
