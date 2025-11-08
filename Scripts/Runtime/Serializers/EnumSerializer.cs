using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Runtime.Serializers
{
    /// <summary>
    /// Serializer for Enum types.
    /// Store as int and deserialize back to Enum.
    /// </summary>
    public class EnumSerializer : IDataSerializer
    {
        public string Serialize(object data, string param)
        {
            if (data == null)
                return string.Empty;
            return data.ToString();
        }

        public object Deserialize(string serializedData, string param)
        {
            if (string.IsNullOrEmpty(serializedData))
                return null;
            
            var enumDict = ODDBEnumUtility.GetEnumValues(param);
            if (enumDict == null)
                return null;
            
            return enumDict.GetValueOrDefault(serializedData);
        }
    }
}