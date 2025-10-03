namespace TeamODD.ODDB.Runtime.Data.Serializers
{
    public interface IODDBDataSerializer
    {
        public string Serialize(object data);
        public object Deserialize(string serializedData);
    }
}