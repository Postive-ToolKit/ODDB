using System;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.MCP.Tools.Data
{
    public class SetRowIdTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public SetRowIdTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_set_row_id";
        public string Description => "Change a row ID in a Table.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["tableId"] = new JObject { ["type"] = "string" },
                ["rowId"] = new JObject { ["type"] = "string" },
                ["newRowId"] = new JObject { ["type"] = "string" },
            },
            ["required"] = new JArray("tableId", "rowId", "newRowId"),
        };

        public object Execute(JToken args)
        {
            var tableId = args?["tableId"]?.ToString()?.Trim();
            var rowId = args?["rowId"]?.ToString()?.Trim();
            var newRowId = args?["newRowId"]?.ToString()?.Trim();
            if (string.IsNullOrEmpty(tableId) || string.IsNullOrEmpty(rowId) || string.IsNullOrEmpty(newRowId))
                throw new McpException(McpErrorKind.InvalidArg, "tableId, rowId, and newRowId required");
            if (_useCase.GetViewByKey(tableId) is not Table table)
                throw new McpException(McpErrorKind.NotFound, $"table not found: {tableId}");
            if (table.GetRow(rowId) == null)
                throw new McpException(McpErrorKind.NotFound, $"row not found: {rowId}");
            if (!string.Equals(rowId, newRowId, StringComparison.Ordinal) && _useCase.GetRow(newRowId) != null)
                throw new McpException(McpErrorKind.Conflict, $"row id already exists: {newRowId}");

            try
            {
                _useCase.SetRowId(tableId, rowId, newRowId);
            }
            catch (InvalidOperationException ex)
            {
                throw new McpException(McpErrorKind.Conflict, ex.Message);
            }

            return new { success = true, tableId, oldRowId = rowId, rowId = newRowId, affectedViewId = tableId };
        }
    }
}
