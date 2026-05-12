using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace TeamODD.ODDB.Editors.MCP.Resources
{
    public class McpResourceRegistry
    {
        private readonly List<IMcpResource> _resources = new List<IMcpResource>();

        public void Register(IMcpResource r) => _resources.Add(r);

        public IMcpResource Match(string uri)
        {
            foreach (var r in _resources)
                if (r.TryMatch(uri)) return r;
            return null;
        }

        public JArray ListAsJson()
        {
            var arr = new JArray();
            foreach (var r in _resources.OrderBy(x => x.UriOrTemplate))
            {
                arr.Add(new JObject
                {
                    ["uri"] = r.UriOrTemplate,
                    ["description"] = r.Description ?? "",
                    ["mimeType"] = r.MimeType ?? "application/json",
                });
            }
            return arr;
        }
    }
}
