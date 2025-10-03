namespace TeamODD.ODDB.Runtime.Data.Serializers
{ 
    /// <summary>
    /// Serializer for string data type.
    /// </summary>
    public class ODDBStringSerializer : IODDBDataSerializer
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