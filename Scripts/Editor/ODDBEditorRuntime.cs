using TeamODD.ODDB.Editors.CLI.Tools;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor;

namespace TeamODD.ODDB.Editors
{
    /// <summary>Owns the shared ODDB editor session used by the window and project-local CLI bridge.</summary>
    [InitializeOnLoad]
    public static class ODDBEditorRuntime
    {
        private static IODDBEditorUseCase _useCase;
        private static ODDBEditorSession _session;
        private static CliOperationRegistry _tools;
        internal const string EmbeddedPackageVersion = "2.9.1";

        static ODDBEditorRuntime()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
        }

        public static IODDBEditorUseCase UseCase
        {
            get
            {
                if (_useCase == null)
                {
                    var instance = new ODDBEditorUseCase();
                    ODDBEditorDI.RegisterSelfAndInterfaces(instance);
                    ODDBEditorDI.RegisterSelfAndInterfaces(instance.DataBase);
                    _useCase = instance;
                }
                return _useCase;
            }
        }

        public static IODDatabase Database => UseCase.DataBase;
        public static ODDBEditorSession Session => _session ??= new ODDBEditorSession(UseCase);

        /// <summary>In-process CLI operations for existing Unity Editor integrations.</summary>
        public static CliOperationRegistry Tools => _tools ??= new CliOperationRegistry(UseCase);

        internal static string ResolvePackageVersion()
        {
            try
            {
                return UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ODDBEditorRuntime).Assembly)?.version
                       ?? EmbeddedPackageVersion;
            }
            catch { return EmbeddedPackageVersion; }
        }

        public static void ReloadDatabase()
        {
            if (UseCase is not ODDBEditorUseCase concrete)
                throw new System.InvalidOperationException("ODDB editor use case is unavailable.");
            concrete.ReloadFromDisk();
        }

        internal static void ResetForTesting() => Dispose();

        private static void Dispose()
        {
            _useCase?.Dispose();
            _useCase = null;
            _session = null;
            _tools = null;
        }
    }
}
