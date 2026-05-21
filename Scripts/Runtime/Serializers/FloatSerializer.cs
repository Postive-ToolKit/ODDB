using TeamODD.ODDB.Runtime.Types;

namespace TeamODD.ODDB.Runtime.Serializers
{
    [ODDBType("float", targetType: typeof(float), folder: "Primitives")]
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