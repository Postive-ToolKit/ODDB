using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Serializers;

namespace TeamODD.ODDB.Runtime.Enums
{
    public enum ODDBDataType
    {
        [DataSerializer(typeof(StringSerializer))]
        String = 0,
        [DataSerializer(typeof(IntSerializer))]
        Int = 1,
        [DataSerializer(typeof(FloatSerializer))]
        Float = 2,
        [DataSerializer(typeof(BoolSerializer))]
        Bool = 3,
        [DataSerializer(typeof(ResourceSerializer))]
        Resources = 1003,
        #if ADDRESSABLE_EXIST
        [DataSerializer(typeof(AddressableSerializer))]
        Addressable = 1004,
        #endif

        // View Reference Type
        // 이건 고민좀 필요하겠네
        [DataSerializer(typeof(StringSerializer))]
        View = 2000,
        [DataTypeOption(true)]
        ID = 2001,
    }
}