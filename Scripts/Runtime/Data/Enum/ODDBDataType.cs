using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Data.Serializers;

namespace TeamODD.ODDB.Runtime.Data.Enum
{
    public enum ODDBDataType
    {
        [ODDBDataSerializer(typeof(ODDBStringSerializer))]
        String = 0,
        [ODDBDataSerializer(typeof(ODDBIntSerializer))]
        Int = 1,
        [ODDBDataSerializer(typeof(ODDBFloatSerializer))]
        Float = 2,
        [ODDBDataSerializer(typeof(ODDBBoolSerializer))]
        Bool = 3,
        
        // Reference Type Start with 1000
        [ODDBDataSerializer(typeof(ODDBResourceScriptableSerializer))]
        ScriptableObject = 1000,
        [ODDBDataSerializer(typeof(ODDBResourcePrefabSerializer))]
        Prefab = 1001,
        [ODDBDataSerializer(typeof(ODDBResourceSpriteSerializer))]
        Sprite = 1002,
        
        // View Reference Type
        // 이건 고민좀 필요하겠네
        [ODDBDataSerializer(typeof(ODDBStringSerializer))]
        View = 2000,
        [ODDBDataTypeOption(true)]
        ID = 2001,
    }
}