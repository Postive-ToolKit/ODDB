using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.MCP.Tools.Schema
{
    public class MoveFieldTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public MoveFieldTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_move_field";
        public string Description => "Reorder a field within a view.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["viewId"] = new JObject { ["type"] = "string" },
                ["oldIndex"] = new JObject { ["type"] = "integer" },
                ["newIndex"] = new JObject { ["type"] = "integer" },
            },
            ["required"] = new JArray("viewId", "oldIndex", "newIndex"),
        };

        public object Execute(JToken args)
        {
            var viewId = args?["viewId"]?.ToString();
            if (string.IsNullOrEmpty(viewId) || args?["oldIndex"] == null || args?["newIndex"] == null)
                throw new McpException(McpErrorKind.InvalidArg, "viewId, oldIndex, newIndex required");
            var view = _useCase.GetViewByKey(viewId);
            if (view == null) throw new McpException(McpErrorKind.NotFound, $"view not found: {viewId}");
            int oldI = args["oldIndex"].ToObject<int>();
            int newI = args["newIndex"].ToObject<int>();
            int n = view.TotalFields.Count;
            if (oldI < 0 || oldI >= n || newI < 0 || newI >= n)
                throw new McpException(McpErrorKind.InvalidArg, "index out of range");
            _useCase.MoveField(viewId, oldI, newI);
            return new { success = true, affectedViewId = viewId };
        }
    }
}
