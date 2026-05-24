using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Types;

namespace TeamODD.ODDB.Editors.MCP.Tools.Schema
{
    public class SetFieldTypeTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public SetFieldTypeTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_set_field_type";
        public string Description => "Change a field's type (and optional param) without removing the column. Cell data is preserved.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["viewId"] = new JObject { ["type"] = "string" },
                ["fieldIndex"] = new JObject { ["type"] = "integer", ["description"] = "Index into the view's TotalFields (including inherited)." },
                ["fieldType"] = new JObject { ["type"] = "string", ["description"] = "ODDB type key (e.g. int/float/bool/string/enum/resource/view/custom)" },
                ["param"] = new JObject { ["type"] = "string", ["description"] = "Optional param (e.g. enum full type name)." },
            },
            ["required"] = new JArray("viewId", "fieldIndex", "fieldType"),
        };

        public object Execute(JToken args)
        {
            var viewId = args?["viewId"]?.ToString();
            var typeStr = args?["fieldType"]?.ToString();
            var param = args?["param"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(viewId) || string.IsNullOrEmpty(typeStr))
                throw new McpException(McpErrorKind.InvalidArg, "viewId and fieldType required");
            if (args?["fieldIndex"] == null)
                throw new McpException(McpErrorKind.InvalidArg, "fieldIndex required");
            int fieldIndex = (int)args["fieldIndex"];

            var view = _useCase.GetViewByKey(viewId);
            if (view == null)
                throw new McpException(McpErrorKind.NotFound, $"view not found: {viewId}");
            if (fieldIndex < 0 || fieldIndex >= view.TotalFields.Count)
                throw new McpException(McpErrorKind.InvalidArg, $"fieldIndex {fieldIndex} out of range (0..{view.TotalFields.Count - 1})");

            var typeKey = typeStr.ToLowerInvariant();
            if (TypeRegistry.GetDescriptor(typeKey) == null)
                throw new McpException(McpErrorKind.InvalidArg, $"unknown fieldType: {typeStr}");

            _useCase.SetFieldType(viewId, fieldIndex, typeKey, param);
            return new { success = true, affectedViewId = viewId, fieldIndex, fieldType = typeKey, param };
        }
    }
}
