using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Params.Interfaces;

namespace TeamODD.ODDB.Runtime.Params
{
    /// <summary>
    /// Sub selector creator for Data Type
    /// </summary>
    #if ADDRESSABLE_EXIST
    [UseSubSelector(ODDBDataType.Resources, ODDBDataType.Addressable)]
    #else
    [UseSubSelector(ODDBDataType.Resources)]
    #endif
    public class ReferenceParamSelector : IFieldParamSelector
    {
        public Dictionary<string, string> GetOptions()
        {
            var result = new Dictionary<string, string>();
            foreach (ODDBReferenceDataType refDataType in Enum.GetValues(typeof(ODDBReferenceDataType)))
                result.Add(refDataType.ToString(), refDataType.ToString());
            return result;
        }
    }
}