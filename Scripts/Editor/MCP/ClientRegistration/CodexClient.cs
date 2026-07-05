using System;
using System.IO;

namespace TeamODD.ODDB.Editors.MCP.ClientRegistration
{
    /// <summary>
    /// Codex stores MCP configuration in <c>~/.codex/config.toml</c> and shares
    /// that file between the CLI and IDE extension.
    /// </summary>
    public class CodexClient : IMcpClient
    {
        public string DisplayName => "Codex";

        public string ConfigPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".codex",
                "config.toml");

        public void Register(string key, string url)
        {
            TomlConfigFile.UpsertHttpMcpServer(ConfigPath, key, url);
        }

        public void Unregister(string key)
        {
            if (!File.Exists(ConfigPath)) return;
            TomlConfigFile.RemoveMcpServer(ConfigPath, key);
        }
    }
}
