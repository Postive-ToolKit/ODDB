using System.IO;
using System.Text;

namespace TeamODD.ODDB.Editors.MCP.ClientRegistration
{
    /// <summary>
    /// Minimal TOML updater for Codex MCP server sections. It preserves
    /// unrelated config and replaces the target server table as a whole.
    /// </summary>
    internal static class TomlConfigFile
    {
        public static void UpsertHttpMcpServer(string path, string key, string url)
        {
            var text = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            bool removed;
            var updated = RemoveMcpServerSection(text, key, out removed).TrimEnd();

            if (updated.Length > 0)
                updated += "\n\n";

            updated += "[mcp_servers." + key + "]\nurl = \"" + EscapeTomlString(url) + "\"\n";
            Save(path, updated);
        }

        public static bool RemoveMcpServer(string path, string key)
        {
            if (!File.Exists(path)) return false;

            var text = File.ReadAllText(path);
            bool removed;
            var updated = RemoveMcpServerSection(text, key, out removed);
            if (removed)
                Save(path, updated.TrimEnd() + "\n");
            return removed;
        }

        private static void Save(string path, string text)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }

        private static string RemoveMcpServerSection(string text, string key, out bool removed)
        {
            removed = false;
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var builder = new StringBuilder();
            var skipping = false;

            foreach (var line in lines)
            {
                string tableName;
                if (TryGetTableName(line, out tableName))
                {
                    skipping = IsTargetOrChildTable(tableName, key);
                    if (skipping)
                    {
                        removed = true;
                        continue;
                    }
                }

                if (!skipping)
                    builder.Append(line).Append('\n');
            }

            return builder.ToString();
        }

        private static bool TryGetTableName(string line, out string tableName)
        {
            tableName = null;
            var trimmed = line.Trim();
            if (trimmed.Length < 3 || !trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
                return false;

            if (trimmed.StartsWith("[[") && trimmed.EndsWith("]]"))
            {
                tableName = trimmed.Substring(2, trimmed.Length - 4).Trim();
                return tableName.Length > 0;
            }

            tableName = trimmed.Substring(1, trimmed.Length - 2).Trim();
            return tableName.Length > 0;
        }

        private static bool IsTargetOrChildTable(string tableName, string key)
        {
            var prefix = "mcp_servers." + key;
            return tableName == prefix || tableName.StartsWith(prefix + ".");
        }

        private static string EscapeTomlString(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}
