using System;
using System.IO;

namespace TeamODD.ODDB.Editors.MCP.ClientRegistration
{
    public class ClaudeCodeClient : IMcpClient
    {
        public string DisplayName => "Claude Code";

        public string ConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude.json");

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
