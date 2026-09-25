using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Enums;

namespace TeamODD.ODDB.Editors.CLI.Tools.Schema
{
    public class RemoveViewTool : ICliOperation
    {
        private readonly IODDBEditorUseCase _useCase;
        public RemoveViewTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_remove_view";
        public string Description => "Remove a View.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject { ["viewId"] = new JObject { ["type"] = "string" } },
            ["required"] = new JArray("viewId"),
        };

        public object Execute(JToken args)
        {
            var id = args?["viewId"]?.ToString();
            if (string.IsNullOrEmpty(id))
                throw new CliOperationException(CliErrorKind.InvalidArg, "viewId required");
            var v = _useCase.GetViewByKey(id);
            if (v == null) throw new CliOperationException(CliErrorKind.NotFound, $"view not found: {id}");
            if (_useCase.GetViewTypeByKey(id) != ODDBViewType.View)
                throw new CliOperationException(CliErrorKind.InvalidArg, $"{id} is not a View; use oddb_remove_table");
            _useCase.RemoveView(id);
            return new { success = true };
        }
    }
}
