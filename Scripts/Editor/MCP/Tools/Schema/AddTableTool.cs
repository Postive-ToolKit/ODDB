using System.Linq;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.MCP.Tools.Schema
{
    public class AddTableTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public AddTableTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_add_table";
        public string Description => "Create a new Table. Optional name, parentViewId, and bindType trigger follow-up commands.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["name"] = new JObject { ["type"] = "string" },
                ["parentViewId"] = new JObject { ["type"] = "string" },
                ["bindType"] = new JObject { ["type"] = "string" },
            },
        };

        public object Execute(JToken args)
        {
            var name = args?["name"]?.ToString();
            var parentId = args?["parentViewId"]?.ToString();
            var bindType = args?["bindType"]?.ToString();

            int beforeCount = _useCase.GetViews()?.Count() ?? 0;
            _useCase.AddTable();
            var view = _useCase.GetViews()?.Skip(beforeCount).FirstOrDefault();
            if (view == null)
                throw new McpException(McpErrorKind.Internal, "AddTable did not produce a new table");

            if (!string.IsNullOrEmpty(name)) _useCase.SetViewName(view.ID, name);
            if (!string.IsNullOrEmpty(parentId)) _useCase.SetViewParent(view.ID, parentId);
            if (!string.IsNullOrEmpty(bindType))
            {
                if (!ODDBTypeUtility.TryConvertBindType(bindType, out var t))
                    throw new McpException(McpErrorKind.InvalidArg, $"bindType cannot be resolved: {bindType}");
                _useCase.SetViewBindType(view.ID, t);
            }

            return new { success = true, tableId = view.ID.ToString(), affectedViewId = view.ID.ToString() };
        }
    }
}
