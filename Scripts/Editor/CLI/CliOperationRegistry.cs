using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.CLI.Tools;
using TeamODD.ODDB.Editors.CLI.Tools.Data;
using TeamODD.ODDB.Editors.CLI.Tools.Schema;
using TeamODD.ODDB.Editors.CLI.Tools.System;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.CLI.Tools
{
    /// <summary>In-process operation catalog shared by the Editor CLI bridge and editor integrations.</summary>
    public sealed class CliOperationRegistry
    {
        private readonly Dictionary<string, ICliOperation> _operations = new Dictionary<string, ICliOperation>();

        public CliOperationRegistry(IODDBEditorUseCase useCase)
        {
            Register(new AddRowTool(useCase));
            Register(new RemoveRowTool(useCase));
            Register(new SetRowIdTool(useCase));
            Register(new SetCellTool(useCase));
            Register(new AddViewTool(useCase));
            Register(new AddTableTool(useCase));
            Register(new RemoveViewTool(useCase));
            Register(new RemoveTableTool(useCase));
            Register(new AddFieldTool(useCase));
            Register(new RemoveFieldTool(useCase));
            Register(new MoveFieldTool(useCase));
            Register(new SetFieldTypeTool(useCase));
            Register(new SetViewNameTool(useCase));
            Register(new SetViewIdTool(useCase));
            Register(new SetViewBindTypeTool(useCase));
            Register(new SetViewParentTool(useCase));
            Register(new GenerateCodeTool(useCase));
            Register(new SaveDatabaseTool(useCase));
        }

        public void Register(ICliOperation operation) => _operations[operation.Name] = operation;
        public IReadOnlyCollection<ICliOperation> All => _operations.Values;
        public bool TryGet(string name, out ICliOperation operation) => _operations.TryGetValue(name, out operation);

        public JArray ListAsJson()
        {
            var items = new JArray();
            foreach (var operation in _operations.Values.OrderBy(item => item.Name))
            {
                items.Add(new JObject
                {
                    ["name"] = operation.Name,
                    ["description"] = operation.Description ?? string.Empty,
                    ["inputSchema"] = operation.InputSchema ?? new JObject { ["type"] = "object" }
                });
            }
            return items;
        }
    }
}
