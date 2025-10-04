namespace TeamODD.ODDB.Runtime.Serializers
{ 
    /// <summary>
    /// Serializer for string data type.
    /// </summary>
    public class StringSerializer : IDataSerializer
    {
        public string Serialize(object data)
        {
            return data.ToString();
        }

        public object Deserialize(string serializedData)
        {
            return serializedData;
        }
    }
}