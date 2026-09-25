using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.CLI.Tools.Schema
{
    public class SetViewParentTool : ICliOperation
    {
        private readonly IODDBEditorUseCase _useCase;
        public SetViewParentTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_set_view_parent";
        public string Description => "Set or clear (null) the parent View of a View/Table.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["viewId"] = new JObject { ["type"] = "string" },
                ["parentViewId"] = new JObject { },
            },
            ["required"] = new JArray("viewId"),
        };

        public object Execute(JToken args)
        {
            var id = args?["viewId"]?.ToString();
            if (string.IsNullOrEmpty(id))
                throw new CliOperationException(CliErrorKind.InvalidArg, "viewId required");
            if (_useCase.GetViewByKey(id) == null)
                throw new CliOperationException(CliErrorKind.NotFound, $"view not found: {id}");

            string parentKey = null;
            var pt = args["parentViewId"];
            if (pt != null && pt.Type != JTokenType.Null)
                parentKey = pt.ToString();

            if (!string.IsNullOrEmpty(parentKey) && _useCase.GetViewByKey(parentKey) == null)
                throw new CliOperationException(CliErrorKind.NotFound, $"parent view not found: {parentKey}");

            _useCase.SetViewParent(id, parentKey);
            return new { success = true, affectedViewId = id };
        }
    }
}
