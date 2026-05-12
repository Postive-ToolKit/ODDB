using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace TeamODD.ODDB.Editors.MCP.Tools
{
    public class McpToolRegistry
    {
        private readonly Dictionary<string, IMcpTool> _tools = new Dictionary<string, IMcpTool>();

        public void Register(IMcpTool tool) => _tools[tool.Name] = tool;

        public IReadOnlyCollection<IMcpTool> All => _tools.Values;

        public bool TryGet(string name, out IMcpTool tool) => _tools.TryGetValue(name, out tool);

        public JArray ListAsJson()
        {
            var arr = new JArray();
            foreach (var t in _tools.Values.OrderBy(x => x.Name))
            {
                arr.Add(new JObject
                {
                    ["name"] = t.Name,
                    ["description"] = t.Description ?? "",
                    ["inputSchema"] = t.InputSchema ?? new JObject { ["type"] = "object" },
                });
            }
            return arr;
        }
    }
}
