using TeamODD.ODDB.Editors.UI.Fields.References;
using TeamODD.ODDB.Runtime.Data.Enum;

namespace TeamODD.ODDB.Editors.UI.Fields
{
    public static class ODDBFieldFactory
    {
        public static IODDBField CreateField(ODDBDataType dataType)
        {
            return dataType switch
            {
                ODDBDataType.String => new ODDBStringField(),
                ODDBDataType.Int => new ODDBNumberField(true),
                ODDBDataType.Float => new ODDBNumberField(false),
                ODDBDataType.Bool => new ODDBBoolField(),
                ODDBDataType.Prefab => new ODDBPrefabField(),
                ODDBDataType.Sprite => new ODDBSpriteField(),
                _ => new ODDBStringField() // 기본값으로 문자열 필드 사용
            };
        }
    }
} 