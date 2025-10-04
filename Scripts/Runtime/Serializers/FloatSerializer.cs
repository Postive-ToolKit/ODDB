namespace TeamODD.ODDB.Runtime.Serializers
{
    public class FloatSerializer : IDataSerializer
    {
        public string Serialize(object data)
        {
            return data.ToString();
        }

        public object Deserialize(string serializedData)
        {
            if (float.TryParse(serializedData, out float result))
                return result;
            return 0f;
        }
    }
}