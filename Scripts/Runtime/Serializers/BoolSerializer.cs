namespace TeamODD.ODDB.Runtime.Serializers
{
    /// <summary>
    /// Serializer for boolean data type.
    /// </summary>
    public class BoolSerializer : IDataSerializer
    {
        public string Serialize(object data, string param)
        {
            return data.ToString();
        }

        public object Deserialize(string serializedData, string param)
        {
            if (bool.TryParse(serializedData, out bool result))
                return result;
            return false;
        }
    }
}