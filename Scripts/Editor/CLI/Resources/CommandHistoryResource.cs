using System.Linq;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.CLI.Resources
{
    public class CommandHistoryResource : ICliResource
    {
        private readonly IODDBEditorUseCase _useCase;
        public CommandHistoryResource(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string UriOrTemplate => "oddb://commands/history";
        public string Description => "Recent command history (undo/redo stacks).";
        public string MimeType => "application/json";

        public bool TryMatch(string uri) => uri == UriOrTemplate;

        public object Read(string uri)
        {
            return new
            {
                undo = _useCase.GetUndoHistory().Select(c => new { name = c.Name, executionTime = c.ExecutionTime }).ToArray(),
                redo = _useCase.GetRedoHistory().Select(c => new { name = c.Name, executionTime = c.ExecutionTime }).ToArray(),
            };
        }
    }
}
