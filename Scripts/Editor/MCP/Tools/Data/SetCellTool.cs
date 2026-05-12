using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.MCP.Tools.Data
{
    public class SetCellTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public SetCellTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_set_cell";
        public string Description => "Set a single cell value. Goes through SetCellDataCommand for undo support.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["tableId"] = new JObject { ["type"] = "string" },
                ["rowId"] = new JObject { ["type"] = "string" },
                ["fieldIndex"] = new JObject { ["type"] = "integer" },
                ["value"] = new JObject { },
            },
            ["required"] = new JArray("tableId", "rowId", "fieldIndex", "value"),
        };

        public object Execute(JToken args)
        {
            var tableId = args?["tableId"]?.ToString();
            var rowId = args?["rowId"]?.ToString();
            if (string.IsNullOrEmpty(tableId) || string.IsNullOrEmpty(rowId))
                throw new McpException(McpErrorKind.InvalidArg, "tableId and rowId required");
            if (args?["fieldIndex"] == null)
                throw new McpException(McpErrorKind.InvalidArg, "fieldIndex required");
            int fieldIndex = args["fieldIndex"].ToObject<int>();

            var view = _useCase.GetViewByKey(tableId);
            if (view is not Table table)
                throw new McpException(McpErrorKind.NotFound, $"table not found: {tableId}");
            if (table.GetRow(rowId) == null)
                throw new McpException(McpErrorKind.NotFound, $"row not found: {rowId}");

            var rawValue = args["value"];
            object value = rawValue?.Type == JTokenType.Null ? null : rawValue?.ToObject<object>();
            _useCase.SetCellData(tableId, rowId, fieldIndex, value);
            return new { success = true, affectedViewId = tableId };
        }
    }
}
