namespace TeamODD.ODDB.Runtime.Data.Serializers
{
    public class ODDBFloatSerializer : IODDBDataSerializer
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