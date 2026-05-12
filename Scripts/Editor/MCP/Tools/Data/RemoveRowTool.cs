using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.MCP.Tools.Data
{
    public class RemoveRowTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public RemoveRowTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_remove_row";
        public string Description => "Remove a row from a table.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["tableId"] = new JObject { ["type"] = "string" },
                ["rowId"] = new JObject { ["type"] = "string" },
            },
            ["required"] = new JArray("tableId", "rowId"),
        };

        public object Execute(JToken args)
        {
            var tableId = args?["tableId"]?.ToString();
            var rowId = args?["rowId"]?.ToString();
            if (string.IsNullOrEmpty(tableId) || string.IsNullOrEmpty(rowId))
                throw new McpException(McpErrorKind.InvalidArg, "tableId and rowId required");
            var view = _useCase.GetViewByKey(tableId);
            if (view is not Table table)
                throw new McpException(McpErrorKind.NotFound, $"table not found: {tableId}");
            if (table.GetRow(rowId) == null)
                throw new McpException(McpErrorKind.NotFound, $"row not found: {rowId}");
            _useCase.RemoveRow(tableId, rowId);
            return new { success = true, affectedViewId = tableId };
        }
    }
}
