using System.Linq;
using TeamODD.ODDB.Runtime.Types;

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
            return TypeRegistry.All
                .OrderBy(t => t.Folder)
                .ThenBy(t => t.Key)
                .Select(t => new
                {
                    name = t.Key,
                    folder = t.Folder,
                    requiresParam = t.RequiresParam,
                })
                .ToArray();
        }
    }
}
