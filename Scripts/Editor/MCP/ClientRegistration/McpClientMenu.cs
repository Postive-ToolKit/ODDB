using System;
using UnityEditor;

namespace TeamODD.ODDB.Editors.MCP.ClientRegistration
{
    /// <summary>
    /// Unity menu entries that auto-write the ODDB MCP server URL into the
    /// supported client config files (Claude Code, Gemini CLI, Codex). The key is
    /// fixed as "oddb"; existing entries with the same key are overwritten.
    /// </summary>
    public static class McpClientMenu
    {
        private const string KEY = "oddb";
        private const string MENU_REGISTER = "ODDB/MCP/Register/";
        private const string MENU_UNREGISTER = "ODDB/MCP/Unregister/";

        [MenuItem(MENU_REGISTER + "Claude Code")]
        public static void RegisterClaude() => Register(new ClaudeCodeClient());

        [MenuItem(MENU_REGISTER + "Gemini CLI")]
        public static void RegisterGemini() => Register(new GeminiClient());

        [MenuItem(MENU_REGISTER + "Codex")]
        public static void RegisterCodex() => Register(new CodexClient());

        [MenuItem(MENU_UNREGISTER + "Claude Code")]
        public static void UnregisterClaude() => Unregister(new ClaudeCodeClient());

        [MenuItem(MENU_UNREGISTER + "Gemini CLI")]
        public static void UnregisterGemini() => Unregister(new GeminiClient());

        [MenuItem(MENU_UNREGISTER + "Codex")]
        public static void UnregisterCodex() => Unregister(new CodexClient());

        private static void Register(IMcpClient client)
        {
            var port = ODDBEditorRuntime.McpPort;
            if (!port.HasValue)
            {
                EditorUtility.DisplayDialog(
                    "ODDB MCP",
                    $"MCP server is not running. Enable it in ODDBEditorSettings, then try again.",
                    "OK");
                return;
            }

            var url = $"http://127.0.0.1:{port.Value}";
            try
            {
                client.Register(KEY, url);
                EditorUtility.DisplayDialog(
                    "ODDB MCP",
                    $"Registered '{KEY}' → {url}\nin {client.ConfigPath}\n\nRestart {client.DisplayName} to pick up the new server.",
                    "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "ODDB MCP",
                    $"Failed to write {client.ConfigPath}:\n{ex.Message}",
                    "OK");
            }
        }

        private static void Unregister(IMcpClient client)
        {
            try
            {
                client.Unregister(KEY);
                EditorUtility.DisplayDialog(
                    "ODDB MCP",
                    $"Removed '{KEY}' from {client.ConfigPath}\n\nRestart {client.DisplayName} to clear the entry.",
                    "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "ODDB MCP",
                    $"Failed to write {client.ConfigPath}:\n{ex.Message}",
                    "OK");
            }
        }
    }
}
