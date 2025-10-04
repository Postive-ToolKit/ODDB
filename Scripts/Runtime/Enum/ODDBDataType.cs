using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Serializers;

namespace TeamODD.ODDB.Runtime.Enum
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
        
        // Reference Type Start with 1000
        [DataSerializer(typeof(ResourceScriptableSerializer))]
        ScriptableObject = 1000,
        [DataSerializer(typeof(ResourcePrefabSerializer))]
        Prefab = 1001,
        [DataSerializer(typeof(ResourceSpriteSerializer))]
        Sprite = 1002,
        
        // View Reference Type
        // 이건 고민좀 필요하겠네
        [DataSerializer(typeof(StringSerializer))]
        View = 2000,
        [DataTypeOption(true)]
        ID = 2001,
    }
}