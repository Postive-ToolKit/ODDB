namespace TeamODD.ODDB.Runtime.Serializers
{
    /// <summary>
    /// Serializer for boolean data type.
    /// </summary>
    public class BoolSerializer : IDataSerializer
    {
        public string Serialize(object data)
        {
            return data.ToString();
        }

        public object Deserialize(string serializedData)
        {
            if (bool.TryParse(serializedData, out bool result))
                return result;
            return false;
        }
    }
}