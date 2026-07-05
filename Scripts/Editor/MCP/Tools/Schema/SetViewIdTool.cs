using System;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.MCP.Tools.Schema
{
    public class SetViewIdTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public SetViewIdTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_set_view_id";
        public string Description => "Change the ID of a View or Table.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["viewId"] = new JObject { ["type"] = "string" },
                ["newViewId"] = new JObject { ["type"] = "string" },
            },
            ["required"] = new JArray("viewId", "newViewId"),
        };

        public object Execute(JToken args)
        {
            var id = args?["viewId"]?.ToString()?.Trim();
            var newId = args?["newViewId"]?.ToString()?.Trim();
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newId))
                throw new McpException(McpErrorKind.InvalidArg, "viewId and newViewId required");
            if (_useCase.GetViewByKey(id) == null)
                throw new McpException(McpErrorKind.NotFound, $"view not found: {id}");
            if (!string.Equals(id, newId, StringComparison.Ordinal) && _useCase.GetViewByKey(newId) != null)
                throw new McpException(McpErrorKind.Conflict, $"view id already exists: {newId}");

            try
            {
                _useCase.SetViewId(id, newId);
            }
            catch (InvalidOperationException ex)
            {
                throw new McpException(McpErrorKind.Conflict, ex.Message);
            }

            return new { success = true, oldViewId = id, viewId = newId, affectedViewId = newId };
        }
    }
}
