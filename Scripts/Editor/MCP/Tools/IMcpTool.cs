using Newtonsoft.Json.Linq;

namespace TeamODD.ODDB.Editors.MCP.Tools
{
    public interface IMcpTool
    {
        string Name { get; }                       // e.g. "oddb_add_row"
        string Description { get; }                // human-readable, for tools/list
        JObject InputSchema { get; }               // JSON Schema for input args
        object Execute(JToken arguments);          // returns result payload; throws McpException for errors
    }
}
