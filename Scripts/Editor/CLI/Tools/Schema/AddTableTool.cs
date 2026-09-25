using System.Linq;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.CLI.Tools.Schema
{
    public class AddTableTool : ICliOperation
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
            if (!string.IsNullOrEmpty(parentId) && _useCase.GetViewByKey(parentId) == null)
                throw new CliOperationException(CliErrorKind.NotFound, $"parent view not found: {parentId}");
            global::System.Type resolvedBindType = null;
            if (!string.IsNullOrEmpty(bindType)
                && !ODDBTypeUtility.TryConvertBindType(bindType, out resolvedBindType))
                throw new CliOperationException(CliErrorKind.InvalidArg, $"bindType cannot be resolved: {bindType}");

            var existingIds = new global::System.Collections.Generic.HashSet<string>(
                _useCase.GetViews()?.Select(item => item.ID.ToString())
                ?? global::System.Linq.Enumerable.Empty<string>());
            _useCase.AddTable();
            var view = _useCase.GetViews()?.FirstOrDefault(item => !existingIds.Contains(item.ID.ToString()));
            if (view == null)
                throw new CliOperationException(CliErrorKind.Internal, "AddTable did not produce a new table");

            if (!string.IsNullOrEmpty(name)) _useCase.SetViewName(view.ID, name);
            if (!string.IsNullOrEmpty(parentId)) _useCase.SetViewParent(view.ID, parentId);
            if (resolvedBindType != null) _useCase.SetViewBindType(view.ID, resolvedBindType);

            return new { success = true, tableId = view.ID.ToString(), affectedViewId = view.ID.ToString() };
        }
    }
}
