namespace TeamODD.ODDB.Runtime.Data.Enum
{
    public enum ODDBDataType
    {
        String = 0,
        Int = 1,
        Float = 2,
        Bool = 3,
        
        // Reference Type Start with 1000
        ScriptableObject = 1000,
        Prefab = 1001,
        Sprite = 1002,
        
        // View Reference Type
        View = 2000,
        ID = 2001,
    }
}