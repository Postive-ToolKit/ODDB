using System.Text.RegularExpressions;
using TeamODD.ODDB.Editors.MCP.Serialization;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.MCP.Resources
{
    public class ViewDetailResource : IMcpResource
    {
        private static readonly Regex DetailRe = new Regex(@"^oddb://views/(?<id>[^/]+)(?<sub>/schema)?$");

        private readonly IODDBEditorUseCase _useCase;
        public ViewDetailResource(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string UriOrTemplate => "oddb://views/{id}";
        public string Description => "View detail (append /schema for fields-only).";
        public string MimeType => "application/json";

        public bool TryMatch(string uri)
        {
            var m = DetailRe.Match(uri ?? "");
            return m.Success && m.Groups["id"].Value != "pure";
        }

        public object Read(string uri)
        {
            var m = DetailRe.Match(uri);
            var id = m.Groups["id"].Value;
            var view = _useCase.GetViewByKey(id);
            if (view == null)
                throw new McpException(McpErrorKind.NotFound, $"view not found: {id}");

            if (m.Groups["sub"].Value == "/schema")
            {
                return new
                {
                    totalFields = ViewJson.Fields(view.TotalFields),
                };
            }

            return new
            {
                id = view.ID.ToString(),
                name = view.Name,
                type = _useCase.GetViewTypeByKey(id).ToString(),
                parentId = view.ParentView?.ID.ToString(),
                bindType = view.BindType?.FullName,
                totalFields = ViewJson.Fields(view.TotalFields),
                scopedFields = ViewJson.Fields(view.ScopedFields),
            };
        }
    }
}
