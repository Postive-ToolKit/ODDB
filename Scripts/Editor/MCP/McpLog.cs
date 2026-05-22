using TeamODD.ODDB.Editors.Settings;
using UnityEngine;

namespace TeamODD.ODDB.Editors.MCP
{
    public static class McpLog
    {
        public static void Info(string msg)
        {
            // Avoid touching the singleton until it's safe (Settings.Setting may
            // create assets on first access during Editor boot).
            bool verbose = false;
            try { verbose = ODDBEditorSettings.Setting != null && ODDBEditorSettings.Setting.MCPServerVerbose; }
            catch { }
            if (!verbose) return;
            Debug.Log($"[ODDB-MCP] {msg}");
        }

        // Always-logged variants for lifecycle events that need to surface
        // regardless of verbose setting (server start/stop, port fallback).
        public static void Lifecycle(string msg) => Debug.Log($"[ODDB-MCP] {msg}");

        public static void Warn(string msg) => Debug.LogWarning($"[ODDB-MCP] {msg}");
        public static void Error(string msg) => Debug.LogError($"[ODDB-MCP] {msg}");
    }
}
