namespace TeamODD.ODDB.Runtime.Data.Serializers
{
    /// <summary>
    /// Serializer for integer data type.
    /// </summary>
    public class ODDBIntSerializer : IODDBDataSerializer
    {
        public string Serialize(object data)
        {
            return data.ToString();
        }

        public object Deserialize(string serializedData)
        {
            if (int.TryParse(serializedData, out int result))
                return result;
            return 0;
        }
    }
}