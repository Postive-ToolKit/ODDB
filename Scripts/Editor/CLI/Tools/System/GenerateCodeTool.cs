using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.CodeGen;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.CLI.Tools.System
{
    public class GenerateCodeTool : ICliOperation
    {
        private readonly IODDBEditorUseCase _useCase;
        public GenerateCodeTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_generate_code";
        public string Description => "Run ODDB code generation. Unity compiles generated classes afterward.";
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
                note = "Generator was invoked. Unity compilation and BindType assignment finish after an assembly reload.",
            };
        }
    }
}
