using System.IO;
using System.Reflection;
using TeamODD.ODDB.Editors;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.CodeGen
{
    /// <summary>
    /// Drains the pending remap queue after every assembly reload.
    /// For each entry: try to resolve the generated class via reflection; if
    /// found, assign it to the View.BindType, save the database, drop the entry.
    /// Entries that cannot resolve yet (e.g. compile error) stay in the queue
    /// for the next reload.
    /// </summary>
    internal static class PendingBindAssigner
    {
        private const string GeneratedNamespace = "ODDB.Generated";

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            // Defer until after the editor finishes its own initialization pass
            // so that the runtime settings asset is loadable.
            EditorApplication.delayCall += DrainQueue;
        }

        private static void DrainQueue()
        {
            EditorApplication.delayCall -= DrainQueue;

            var entries = PendingRemapStore.Load();
            if (entries.Count == 0)
                return;

            var settings = ODDBRuntimeSettings.TryLoad();
            if (settings == null)
            {
                Debug.LogWarning("[ODDB CodeGen] RuntimeSettings missing — pending remap deferred.");
                return;
            }
            var dbPath = Path.Combine(settings.Path, settings.DBName);

            var useCase = ODDBEditorRuntime.UseCase;
            if (useCase == null || useCase.DataBase is not ODDatabase database)
            {
                Debug.LogWarning("[ODDB CodeGen] UseCase not ready — pending remap deferred.");
                EditorApplication.delayCall += DrainQueue; // retry next reload
                return;
            }

            bool dirty = false;
            var remaining = new System.Collections.Generic.List<PendingRemapEntry>();

            foreach (var entry in entries)
            {
                var type = ResolveType(entry.className);
                if (type == null)
                {
                    remaining.Add(entry);
                    continue;
                }

                var view = database.GetView(new ODDBID(entry.viewId));
                if (view == null)
                    continue;

                if (view.BindType == type)
                    continue;

                view.BindType = type;
                dirty = true;
            }

            if (dirty)
            {
                try
                {
                    useCase.SaveDatabase(dbPath);
                    AssetDatabase.Refresh();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[ODDB CodeGen] Save failed after BindType assignment: {ex.Message}");
                    return;
                }
            }

            PendingRemapStore.Save(remaining);
            if (entries.Count != remaining.Count)
                Debug.Log($"[ODDB CodeGen] BindType assigned for {entries.Count - remaining.Count} view(s).");
        }

        private static System.Type ResolveType(string className)
        {
            if (string.IsNullOrEmpty(className))
                return null;

            var fullName = $"{GeneratedNamespace}.{className}";
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName, throwOnError: false);
                if (t != null)
                    return t;
            }
            return null;
        }
    }
}
