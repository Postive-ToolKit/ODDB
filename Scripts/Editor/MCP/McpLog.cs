using TeamODD.ODDB.Runtime.Settings;
using UnityEngine;

namespace TeamODD.ODDB.Editors.MCP
{
    public static class McpLog
    {
        public static void Info(string msg)
        {
            // Avoid touching the singleton until it's safe (Settings.Setting may
            // create assets on first access during Editor boot).
            try
            {
                if (ODDBSettings.Setting != null && !ODDBSettings.Setting.MCPServerVerbose) return;
            }
            catch
            {
                return;
            }
            Debug.Log($"[ODDB-MCP] {msg}");
        }

        public static void Warn(string msg) => Debug.LogWarning($"[ODDB-MCP] {msg}");
        public static void Error(string msg) => Debug.LogError($"[ODDB-MCP] {msg}");
    }
}
