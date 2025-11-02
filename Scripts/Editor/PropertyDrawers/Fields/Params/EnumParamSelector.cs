using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Params.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Runtime.Params
{
    /// <summary>
    /// Sub selector creator for Data Type
    /// </summary>
    [UseSubSelector(ODDBDataType.Enum)]
    public class EnumParamSelector : IFieldParamSelector
    {
        public Dictionary<string, string> GetOptions()
        {
            var enumTypes = ODDBEnumUtility.GetAllOddbEnumTypes();
            var options = new Dictionary<string, string>();
            foreach (var enumType in enumTypes)
                options.Add(enumType.Name, enumType.Name);
            return options;
        }
    }
}