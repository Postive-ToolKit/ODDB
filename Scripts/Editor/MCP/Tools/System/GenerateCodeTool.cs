using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.CodeGen;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.MCP.Tools.System
{
    public class GenerateCodeTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public GenerateCodeTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_generate_code";
        public string Description => "Run ODDB code generation. Compilation is async; check editor_state.isCompiling for completion.";
        public JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["viewIds"] = new JObject
                {
                    ["type"] = "array",
                    ["items"] = new JObject { ["type"] = "string" },
                },
            },
        };

        public object Execute(JToken args)
        {
            List<string> viewIds = null;
            if (args?["viewIds"] is JArray a)
                viewIds = a.Select(x => x.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            if (viewIds == null || viewIds.Count == 0)
                ODDBCodeGenerator.GenerateAll();
            else
                ODDBCodeGenerator.GenerateSelection(viewIds);

            return new
            {
                success = true,
                triggered = true,
                note = "Generator was invoked. Unity compilation runs asynchronously — poll editor_state.isCompiling or read_console for status. BindType auto-assignment runs after the next domain reload.",
            };
        }
    }
}
