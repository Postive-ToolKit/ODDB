#if ADDRESSABLE_EXIST
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Types;

namespace TeamODD.ODDB.Runtime.Serializers
{
    [ODDBType(
        "addressable",
        ODDBLoadType.Async,
        targetType: typeof(UnityEngine.Object),
        folder: "Unity Assets",
        requiresParam: true)]
    public class AddressableSerializer : IDataSerializer
    {
        public virtual string Serialize(object data, string param)
        {
            #if UNITY_EDITOR
            ODDB.Logger.Warn($"{nameof(AddressableSerializer)}.{nameof(Serialize)} runtime serialization is not supported.");
            #endif
            return string.Empty;
        }

        public virtual object Deserialize(string serializedData, string param)
        {
            // Async types preserve their serialized key during database porting.
            // Actual asset materialization is delegated to the registered IAsyncLoader.
            return serializedData;
        }
    }
}
#endif
