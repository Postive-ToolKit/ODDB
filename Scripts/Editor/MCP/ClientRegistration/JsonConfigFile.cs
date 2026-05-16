using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TeamODD.ODDB.Editors.MCP.ClientRegistration
{
    /// <summary>
    /// Shared loader/writer for the JSON config files used by MCP-aware client
    /// apps (Claude Code, Gemini CLI, ...). Preserves unrelated top-level keys
    /// and uses indented formatting so the file remains human-friendly.
    /// </summary>
    internal static class JsonConfigFile
    {
        public static JObject Load(string path)
        {
            if (!File.Exists(path)) return new JObject();
            var text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text)) return new JObject();
            return JObject.Parse(text);
        }

        public static void Save(string path, JObject root)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, root.ToString(Formatting.Indented));
        }

        /// <summary>Adds or replaces <c>mcpServers[key] = { "url": url }</c>.</summary>
        public static void UpsertMcpServer(JObject root, string key, string url)
        {
            var servers = root["mcpServers"] as JObject;
            if (servers == null)
            {
                servers = new JObject();
                root["mcpServers"] = servers;
            }
            servers[key] = new JObject { ["url"] = url };
        }

        /// <summary>Removes <c>mcpServers[key]</c> if present. Returns true if it existed.</summary>
        public static bool RemoveMcpServer(JObject root, string key)
        {
            var servers = root["mcpServers"] as JObject;
            if (servers == null) return false;
            return servers.Remove(key);
        }
    }
}
