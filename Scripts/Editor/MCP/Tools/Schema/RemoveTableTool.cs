using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Enums;

namespace TeamODD.ODDB.Editors.MCP.Tools.Schema
{
    public class RemoveTableTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public RemoveTableTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_remove_table";
        public string Description => "Remove a Table.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject { ["tableId"] = new JObject { ["type"] = "string" } },
            ["required"] = new JArray("tableId"),
        };

        public object Execute(JToken args)
        {
            var id = args?["tableId"]?.ToString();
            if (string.IsNullOrEmpty(id))
                throw new McpException(McpErrorKind.InvalidArg, "tableId required");
            var v = _useCase.GetViewByKey(id);
            if (v == null) throw new McpException(McpErrorKind.NotFound, $"table not found: {id}");
            if (_useCase.GetViewTypeByKey(id) != ODDBViewType.Table)
                throw new McpException(McpErrorKind.InvalidArg, $"{id} is not a Table; use oddb_remove_view");
            _useCase.RemoveTable(id);
            return new { success = true };
        }
    }
}
