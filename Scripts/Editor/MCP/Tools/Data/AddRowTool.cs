using System.Linq;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.MCP.Tools.Data
{
    public class AddRowTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public AddRowTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_add_row";
        public string Description => "Append a new row to a table. Returns the new row id.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject { ["tableId"] = new JObject { ["type"] = "string" } },
            ["required"] = new JArray("tableId"),
        };

        public object Execute(JToken args)
        {
            var tableId = args?["tableId"]?.ToString();
            if (string.IsNullOrEmpty(tableId))
                throw new McpException(McpErrorKind.InvalidArg, "tableId required");
            var view = _useCase.GetViewByKey(tableId);
            if (view is not Table table)
                throw new McpException(McpErrorKind.NotFound, $"table not found: {tableId}");

            int before = table.Rows.Count;
            _useCase.AddRow(tableId);
            var newRow = table.Rows.Skip(before).FirstOrDefault();
            return new { success = true, rowId = newRow?.ID.ToString(), affectedViewId = tableId };
        }
    }
}
