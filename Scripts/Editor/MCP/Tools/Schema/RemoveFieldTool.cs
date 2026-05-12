using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.MCP.Tools.Schema
{
    public class RemoveFieldTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public RemoveFieldTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_remove_field";
        public string Description => "Remove a field from a view by index (relative to TotalFields).";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["viewId"] = new JObject { ["type"] = "string" },
                ["index"] = new JObject { ["type"] = "integer" },
            },
            ["required"] = new JArray("viewId", "index"),
        };

        public object Execute(JToken args)
        {
            var viewId = args?["viewId"]?.ToString();
            if (string.IsNullOrEmpty(viewId) || args?["index"] == null)
                throw new McpException(McpErrorKind.InvalidArg, "viewId and index required");
            int idx = args["index"].ToObject<int>();
            var view = _useCase.GetViewByKey(viewId);
            if (view == null) throw new McpException(McpErrorKind.NotFound, $"view not found: {viewId}");
            if (idx < 0 || idx >= view.TotalFields.Count)
                throw new McpException(McpErrorKind.InvalidArg, $"index out of range: {idx}");
            _useCase.RemoveField(viewId, idx);
            return new { success = true, affectedViewId = viewId };
        }
    }
}
