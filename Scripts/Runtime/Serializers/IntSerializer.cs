namespace TeamODD.ODDB.Runtime.Serializers
{
    /// <summary>
    /// Serializer for integer data type.
    /// </summary>
    public class IntSerializer : IDataSerializer
    {
        public string Serialize(object data, string param)
        {
            return data.ToString();
        }

        public object Deserialize(string serializedData, string param)
        {
            if (int.TryParse(serializedData, out int result))
                return result;
            return 0;
        }
    }
}