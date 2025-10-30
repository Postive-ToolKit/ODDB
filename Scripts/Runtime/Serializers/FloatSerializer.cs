namespace TeamODD.ODDB.Runtime.Serializers
{
    public class FloatSerializer : IDataSerializer
    {
        public string Serialize(object data, string param)
        {
            return data.ToString();
        }

        public object Deserialize(string serializedData, string param)
        {
            if (float.TryParse(serializedData, out float result))
                return result;
            return 0f;
        }
    }
}