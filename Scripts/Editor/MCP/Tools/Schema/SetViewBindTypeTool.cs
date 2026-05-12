using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.MCP.Tools.Schema
{
    public class SetViewBindTypeTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public SetViewBindTypeTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_set_view_bind_type";
        public string Description => "Set or clear (null) the bound C# type for a View/Table.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["viewId"] = new JObject { ["type"] = "string" },
                ["typeName"] = new JObject { },
            },
            ["required"] = new JArray("viewId"),
        };

        public object Execute(JToken args)
        {
            var id = args?["viewId"]?.ToString();
            if (string.IsNullOrEmpty(id))
                throw new McpException(McpErrorKind.InvalidArg, "viewId required");
            if (_useCase.GetViewByKey(id) == null)
                throw new McpException(McpErrorKind.NotFound, $"view not found: {id}");

            var typeToken = args["typeName"];
            if (typeToken == null || typeToken.Type == JTokenType.Null)
            {
                _useCase.SetViewBindType(id, null);
                return new { success = true, affectedViewId = id };
            }

            var typeName = typeToken.ToString();
            if (!ODDBTypeUtility.TryConvertBindType(typeName, out var t))
                throw new McpException(McpErrorKind.InvalidArg, $"typeName cannot be resolved: {typeName}");
            _useCase.SetViewBindType(id, t);
            return new { success = true, affectedViewId = id };
        }
    }
}
