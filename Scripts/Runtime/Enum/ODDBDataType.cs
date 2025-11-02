using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Serializers;

namespace TeamODD.ODDB.Runtime.Enums
{
    public enum ODDBDataType
    {
        // Numeric Type
        [DataSerializer(typeof(IntSerializer))]
        Int = 1,
        [DataSerializer(typeof(FloatSerializer))]
        Float = 2,
        
        // Date Type
        [DataSerializer(typeof(EnumSerializer))]
        Enum = 100,
        
        // Logical Type
        [DataSerializer(typeof(BoolSerializer))]
        Bool = 200,
        
        // Text Type
        [DataSerializer(typeof(StringSerializer))]
        String = 300,
        
        // Reference Type
        [DataSerializer(typeof(ResourceSerializer))]
        Resources = 1003,
        #if ADDRESSABLE_EXIST
        [DataSerializer(typeof(AddressableSerializer))]
        Addressable = 1004,
        #endif

        // View Reference Type
        [DataSerializer(typeof(StringSerializer))]
        View = 2000,
        
        // Deprecated Types
        [DataTypeOption(true)]
        ID = 3000,
    }
}