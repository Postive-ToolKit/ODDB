namespace TeamODD.ODDB.Runtime.Serializers
{
    public interface IDataSerializer
    {
        public string Serialize(object data);
        public object Deserialize(string serializedData);
    }
}