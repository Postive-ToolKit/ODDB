namespace TeamODD.ODDB.Runtime.Enums
{
    public static class ODDBDataTypeExtensions
    {
        public static string ToWireKey(this ODDBDataType type)
        {
            switch (type)
            {
                case ODDBDataType.Int:       return "int";
                case ODDBDataType.Float:     return "float";
                case ODDBDataType.Bool:      return "bool";
                case ODDBDataType.String:    return "string";
                case ODDBDataType.Enum:      return "enum";
                case ODDBDataType.Resources: return "resource";
#if ADDRESSABLE_EXIST
                case ODDBDataType.Addressable: return "addressable";
#endif
                case ODDBDataType.View:      return "view";
                case ODDBDataType.ID:        return "id";
                case ODDBDataType.Custom:    return "";
                default: return type.ToString().ToLowerInvariant();
            }
        }
    }
}
