using System;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;

namespace TeamODD.ODDB.Editors.MCP.Tools.Schema
{
    public class AddFieldTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public AddFieldTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_add_field";
        public string Description => "Append a field to a view.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["viewId"] = new JObject { ["type"] = "string" },
                ["fieldName"] = new JObject { ["type"] = "string" },
                ["fieldType"] = new JObject { ["type"] = "string", ["description"] = "ODDBDataType enum name (Int/Float/Bool/String/Enum/Resources/View/Custom)" },
                ["param"] = new JObject { ["type"] = "string" },
            },
            ["required"] = new JArray("viewId", "fieldName", "fieldType"),
        };

        public object Execute(JToken args)
        {
            var viewId = args?["viewId"]?.ToString();
            var fieldName = args?["fieldName"]?.ToString();
            var typeStr = args?["fieldType"]?.ToString();
            var param = args?["param"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(viewId) || string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(typeStr))
                throw new McpException(McpErrorKind.InvalidArg, "viewId, fieldName, fieldType required");
            if (_useCase.GetViewByKey(viewId) == null)
                throw new McpException(McpErrorKind.NotFound, $"view not found: {viewId}");
            if (!Enum.TryParse<ODDBDataType>(typeStr, ignoreCase: true, out var dt))
                throw new McpException(McpErrorKind.InvalidArg, $"unknown fieldType: {typeStr}");

            var field = new Field(fieldName, new FieldType { Type = dt, Param = param });
            _useCase.AddField(viewId, field);
            return new { success = true, affectedViewId = viewId };
        }
    }
}
