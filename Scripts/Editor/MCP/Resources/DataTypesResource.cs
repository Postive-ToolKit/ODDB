using System;
using System.Linq;
using TeamODD.ODDB.Runtime.Enums;

namespace TeamODD.ODDB.Editors.MCP.Resources
{
    public class DataTypesResource : IMcpResource
    {
        public string UriOrTemplate => "oddb://data-types";
        public string Description => "Available ODDB field data types (built-in + custom).";
        public string MimeType => "application/json";

        public bool TryMatch(string uri) => uri == UriOrTemplate;

        public object Read(string uri)
        {
            var values = Enum.GetValues(typeof(ODDBDataType)).Cast<ODDBDataType>();
            return values.Select(v => new
            {
                name = v.ToString(),
                requiresParam = v == ODDBDataType.Enum || v == ODDBDataType.View || v == ODDBDataType.Custom,
            }).ToArray();
        }
    }
}
