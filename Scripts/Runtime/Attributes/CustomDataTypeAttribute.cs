using System;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Attribute to register a custom data type for ODDB.
    /// Uses the provided C# Type as the primary mapping key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CustomDataTypeAttribute : Attribute
    {
        public Type DataType { get; }
        public string TypeID { get; }
        
        public CustomDataTypeAttribute(Type dataType, string typeID = null)
        {
            DataType = dataType;
            // ID가 명시되지 않으면 타입의 전체 이름을 식별자로 사용
            TypeID = string.IsNullOrEmpty(typeID) ? dataType.FullName : typeID;
        }
    }
}