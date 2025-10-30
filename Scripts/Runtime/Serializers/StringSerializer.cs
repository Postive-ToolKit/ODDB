namespace TeamODD.ODDB.Runtime.Serializers
{ 
    /// <summary>
    /// Serializer for string data type.
    /// </summary>
    public class StringSerializer : IDataSerializer
    {
        public string Serialize(object data, string param)
        {
            return data.ToString();
        }

        public object Deserialize(string serializedData, string param)
        {
            return serializedData;
        }
    }
}