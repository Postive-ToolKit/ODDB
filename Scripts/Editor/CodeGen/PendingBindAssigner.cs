using System.IO;
using System.Reflection;
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
            // so that ODDBSettings is loadable.
            EditorApplication.delayCall += DrainQueue;
        }

        private static void DrainQueue()
        {
            EditorApplication.delayCall -= DrainQueue;

            var entries = PendingRemapStore.Load();
            if (entries.Count == 0)
                return;

            var settings = ODDBSettings.Setting;
            if (settings == null)
                return;
            var dbPath = Path.Combine(settings.Path, settings.DBName);

            var dataService = new ODDBDataService();
            if (!dataService.LoadDatabase(dbPath, out var database) || database == null)
            {
                Debug.LogWarning($"[ODDB CodeGen] Pending bind drain skipped — failed to load DB at {dbPath}");
                return;
            }

            bool dirty = false;
            var remaining = new System.Collections.Generic.List<PendingRemapEntry>();

            foreach (var entry in entries)
            {
                var type = ResolveType(entry.className);
                if (type == null)
                {
                    remaining.Add(entry); // try again on next reload
                    continue;
                }

                var view = database.GetView(new ODDBID(entry.viewId));
                if (view == null)
                    continue; // view deleted by user; drop entry silently

                if (view.BindType == type)
                    continue; // already correct; drop

                view.BindType = type;
                dirty = true;
            }

            if (dirty)
            {
                if (!dataService.SaveDatabase(database, dbPath))
                {
                    Debug.LogWarning("[ODDB CodeGen] Failed to save database after BindType assignment.");
                    return;
                }
                AssetDatabase.Refresh();
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
