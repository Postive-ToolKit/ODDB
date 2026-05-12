using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.MCP.Resources
{
    public class TableInheritedResource : IMcpResource
    {
        private static readonly Regex Re = new Regex(@"^oddb://tables/(?<id>[^/]+)/inherited$");

        private readonly IODDBEditorUseCase _useCase;
        public TableInheritedResource(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string UriOrTemplate => "oddb://tables/{id}/inherited";
        public string Description => "Tables inheriting from this view.";
        public string MimeType => "application/json";

        public bool TryMatch(string uri) => Re.IsMatch(uri ?? "");

        public object Read(string uri)
        {
            var id = Re.Match(uri).Groups["id"].Value;
            var view = _useCase.GetViewByKey(id);
            if (view == null) throw new McpException(McpErrorKind.NotFound, $"view not found: {id}");
            var arr = new JArray();
            foreach (var t in _useCase.GetInheritedTables(id))
            {
                arr.Add(new JObject
                {
                    ["id"] = t.ID.ToString(),
                    ["name"] = t.Name,
                });
            }
            return arr;
        }
    }
}
