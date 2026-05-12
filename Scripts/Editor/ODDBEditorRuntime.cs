using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor;

namespace TeamODD.ODDB.Editors
{
    /// <summary>
    /// Editor-process singleton that owns the IODDBEditorUseCase instance.
    /// Previously the window owned this; extracting it allows the MCP server
    /// (and any other editor-level subsystem) to share the same use case and
    /// database state so external mutations show up in the window immediately.
    /// </summary>
    [InitializeOnLoad]
    public static class ODDBEditorRuntime
    {
        private static IODDBEditorUseCase _useCase;

        public static IODDBEditorUseCase UseCase
        {
            get
            {
                if (_useCase == null)
                {
                    _useCase = new ODDBEditorUseCase();
                    ODDBEditorDI.RegisterSelfAndInterfaces(_useCase);
                    ODDBEditorDI.RegisterSelfAndInterfaces(_useCase.DataBase);
                }
                return _useCase;
            }
        }

        public static IODDatabase Database => UseCase.DataBase;

        static ODDBEditorRuntime()
        {
            // The use case is created lazily on first access so editor startup
            // doesn't trigger any UI (e.g. path picker) before the editor is
            // fully ready. The MCP server bootstrap will be added in Task 8.
        }

        // For tests only — drops the singleton so the next access rebuilds it.
        internal static void ResetForTesting()
        {
            _useCase?.Dispose();
            _useCase = null;
        }
    }
}
