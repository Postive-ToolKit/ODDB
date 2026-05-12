using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.MCP.Serialization;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.MCP.Resources
{
    public class TableRowsResource : IMcpResource
    {
        private static readonly Regex Re = new Regex(@"^oddb://tables/(?<id>[^/]+)/rows(?:/(?<rowId>[^/]+))?$");

        private readonly IODDBEditorUseCase _useCase;
        public TableRowsResource(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string UriOrTemplate => "oddb://tables/{id}/rows";
        public string Description => "Rows of a table (append /{rowId} for single row).";
        public string MimeType => "application/json";

        public bool TryMatch(string uri) => Re.IsMatch(uri ?? "");

        public object Read(string uri)
        {
            var m = Re.Match(uri);
            var id = m.Groups["id"].Value;
            var view = _useCase.GetViewByKey(id);
            if (view is not Table table)
                throw new McpException(McpErrorKind.NotFound, $"table not found: {id}");

            var rowId = m.Groups["rowId"].Value;
            if (!string.IsNullOrEmpty(rowId))
            {
                var row = table.GetRow(rowId);
                if (row == null) throw new McpException(McpErrorKind.NotFound, $"row not found: {rowId}");
                return RowJson.Row(row);
            }

            var arr = new JArray();
            foreach (var r in table.Rows) arr.Add(RowJson.Row(r));
            return new { rows = arr };
        }
    }
}
