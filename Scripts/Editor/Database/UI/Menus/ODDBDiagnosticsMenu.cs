using System.IO;
using System.IO.Compression;
using System.Text;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI.Menus
{
    /// <summary>
    /// Editor-only diagnostics for ODDB load issues. Useful when a freshly-loaded
    /// database appears empty in the editor — these helpers expose what's actually
    /// on disk and what the deserializer produced.
    /// </summary>
    internal static class ODDBDiagnosticsMenu
    {
        private const string MENU_PREFIX = ODDBEditorConst.MENU_ROOT + "Diagnose/";

        [MenuItem(MENU_PREFIX + "Dump Loaded Database")]
        private static void DumpLoaded()
        {
            var useCase = ODDBEditorRuntime.UseCase as ODDBEditorUseCase;
            if (useCase == null)
            {
                Debug.LogError("[ODDB] UseCase not initialized.");
                return;
            }
            var path = ODDBRuntimeSettings.ResolveDatabasePath();
            useCase.DumpLoadDiagnostics(path);
        }

        [MenuItem(MENU_PREFIX + "Dump Raw JSON (decompressed)")]
        private static void DumpRawJson()
        {
            var path = ODDBRuntimeSettings.ResolveDatabasePath();
            if (!File.Exists(path))
            {
                Debug.LogError($"[ODDB] File not found at {path}");
                return;
            }

            var bytes = File.ReadAllBytes(path);
            string json;
            try { json = Decompress(bytes); }
            catch (System.Exception)
            {
                // Maybe the file is not gzipped — try raw UTF-8.
                try { json = Encoding.UTF8.GetString(bytes); }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ODDB] Decompress + raw read both failed: {ex.Message}");
                    return;
                }
            }

            Debug.Log($"[ODDB] Raw JSON ({json.Length} chars, path={path}). Preview:");
            // Console truncates very long messages; emit head + tail separately.
            const int chunk = 4000;
            if (json.Length <= chunk)
            {
                Debug.Log(json);
            }
            else
            {
                Debug.Log("--- head ---\n" + json.Substring(0, chunk));
                Debug.Log("--- tail ---\n" + json.Substring(json.Length - chunk));
            }

            // Also dump to a side file for full inspection.
            var dumpPath = Path.Combine(Path.GetTempPath(), $"oddb-dump-{System.Guid.NewGuid():N}.json");
            File.WriteAllText(dumpPath, json);
            Debug.Log($"[ODDB] Full JSON written to {dumpPath}");
            EditorUtility.RevealInFinder(dumpPath);
        }

        [MenuItem(MENU_PREFIX + "Reload from disk")]
        private static void ReloadFromDisk()
        {
            ODDBEditorRuntime.ReloadDatabase();
            // Force any open ODDB Editor window to rebuild against the new instance.
            var existing = Resources.FindObjectsOfTypeAll<ODDBEditorWindow>();
            foreach (var w in existing)
                w.Close();
            EditorApplication.delayCall += () => EditorWindow.GetWindow<ODDBEditorWindow>();
        }

        private static string Decompress(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var gz = new GZipStream(ms, CompressionMode.Decompress);
            using var sr = new StreamReader(gz, Encoding.UTF8);
            return sr.ReadToEnd();
        }
    }
}
