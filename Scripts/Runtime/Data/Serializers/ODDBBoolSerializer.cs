namespace TeamODD.ODDB.Runtime.Data.Serializers
{
    /// <summary>
    /// Serializer for boolean data type.
    /// </summary>
    public class ODDBBoolSerializer : IODDBDataSerializer
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