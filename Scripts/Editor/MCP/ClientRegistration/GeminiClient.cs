using System;
using System.IO;

namespace TeamODD.ODDB.Editors.MCP.ClientRegistration
{
    /// <summary>
    /// Gemini CLI stores its config under <c>~/.gemini/settings.json</c> and
    /// uses the same standard <c>mcpServers</c> shape as Claude Code.
    /// </summary>
    public class GeminiClient : IMcpClient
    {
        public string DisplayName => "Gemini";

        public string ConfigPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "settings.json");

        public void Register(string key, string url)
        {
            var root = JsonConfigFile.Load(ConfigPath);
            JsonConfigFile.UpsertMcpServer(root, key, url);
            JsonConfigFile.Save(ConfigPath, root);
        }

        public void Unregister(string key)
        {
            if (!File.Exists(ConfigPath)) return;
            var root = JsonConfigFile.Load(ConfigPath);
            if (JsonConfigFile.RemoveMcpServer(root, key))
                JsonConfigFile.Save(ConfigPath, root);
        }
    }
}
