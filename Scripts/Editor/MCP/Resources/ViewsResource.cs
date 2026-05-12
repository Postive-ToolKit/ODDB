using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Enums;

namespace TeamODD.ODDB.Editors.MCP.Resources
{
    public class ViewsResource : IMcpResource
    {
        private readonly IODDBEditorUseCase _useCase;
        public ViewsResource(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string UriOrTemplate => "oddb://views";
        public string Description => "List of all ODDB views and tables. Use oddb://views/pure for views only.";
        public string MimeType => "application/json";

        public bool TryMatch(string uri) => uri == "oddb://views" || uri == "oddb://views/pure";

        public object Read(string uri)
        {
            bool pureOnly = uri == "oddb://views/pure";
            var arr = new JArray();
            var views = _useCase.GetViews();
            if (views == null) return arr;
            foreach (var v in views)
            {
                var type = _useCase.GetViewTypeByKey(v.ID);
                if (pureOnly && type == ODDBViewType.Table) continue;
                arr.Add(new JObject
                {
                    ["id"] = v.ID.ToString(),
                    ["name"] = v.Name,
                    ["type"] = type.ToString(),
                    ["parentId"] = v.ParentView?.ID.ToString(),
                    ["bindType"] = v.BindType?.FullName,
                });
            }
            return arr;
        }
    }
}
