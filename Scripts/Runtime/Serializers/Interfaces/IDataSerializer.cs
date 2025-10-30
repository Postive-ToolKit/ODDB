namespace TeamODD.ODDB.Runtime.Serializers
{
    public interface IDataSerializer
    {
        public string Serialize(object data, string param);
        public object Deserialize(string serializedData, string param);
    }
}