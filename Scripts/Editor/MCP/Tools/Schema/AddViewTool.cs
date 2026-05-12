using System.Linq;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.MCP.Tools.Schema
{
    public class AddViewTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public AddViewTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_add_view";
        public string Description => "Create a new View. Optional name triggers a follow-up SetViewName command.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject { ["name"] = new JObject { ["type"] = "string" } },
        };

        public object Execute(JToken args)
        {
            var name = args?["name"]?.ToString();
            int beforeCount = _useCase.GetViews()?.Count() ?? 0;
            _useCase.AddView();
            var view = _useCase.GetViews()?.Skip(beforeCount).FirstOrDefault();
            if (view == null)
                throw new McpException(McpErrorKind.Internal, "AddView did not produce a new view");
            if (!string.IsNullOrEmpty(name))
                _useCase.SetViewName(view.ID, name);
            return new { success = true, viewId = view.ID.ToString(), affectedViewId = view.ID.ToString() };
        }
    }
}
