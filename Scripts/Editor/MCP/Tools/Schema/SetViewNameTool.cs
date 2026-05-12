using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.MCP.Tools.Schema
{
    public class SetViewNameTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public SetViewNameTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_set_view_name";
        public string Description => "Rename a View or Table.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["viewId"] = new JObject { ["type"] = "string" },
                ["name"] = new JObject { ["type"] = "string" },
            },
            ["required"] = new JArray("viewId", "name"),
        };

        public object Execute(JToken args)
        {
            var id = args?["viewId"]?.ToString();
            var name = args?["name"]?.ToString();
            if (string.IsNullOrEmpty(id) || name == null)
                throw new McpException(McpErrorKind.InvalidArg, "viewId and name required");
            if (_useCase.GetViewByKey(id) == null)
                throw new McpException(McpErrorKind.NotFound, $"view not found: {id}");
            _useCase.SetViewName(id, name);
            return new { success = true, affectedViewId = id };
        }
    }
}
