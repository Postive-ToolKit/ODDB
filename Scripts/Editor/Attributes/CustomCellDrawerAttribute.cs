using System;

namespace TeamODD.ODDB.Editors.Attributes
{
    /// <summary>
    /// Attribute to register a custom cell drawer for a specific C# Type in ODDB.
    /// It automatically finds the corresponding serializer registered for the same Type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CustomCellDrawerAttribute : Attribute
    {
        public Type TargetType { get; }
        public string TypeID { get; }

        public CustomCellDrawerAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        // 특정 ID에 명시적으로 연결하고 싶을 때 (하위 호환성용)
        public CustomCellDrawerAttribute(string typeID)
        {
            TypeID = typeID;
        }
    }
}