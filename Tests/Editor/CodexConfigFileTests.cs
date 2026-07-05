using System.IO;
using NUnit.Framework;
using TeamODD.ODDB.Editors.MCP.ClientRegistration;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class CodexConfigFileTests
    {
        [Test]
        public void UpsertHttpMcpServer_AddsCodexMcpServerWithoutRemovingExistingConfig()
        {
            var path = CreateTempConfig(
                "model = \"gpt-5\"\n" +
                "\n" +
                "[mcp_servers.context7]\n" +
                "command = \"npx\"\n" +
                "args = [\"-y\", \"@upstash/context7-mcp\"]\n");

            TomlConfigFile.UpsertHttpMcpServer(path, "oddb", "http://127.0.0.1:9123");

            var config = File.ReadAllText(path);
            StringAssert.Contains("model = \"gpt-5\"", config);
            StringAssert.Contains("[mcp_servers.context7]", config);
            StringAssert.Contains("[mcp_servers.oddb]", config);
            StringAssert.Contains("url = \"http://127.0.0.1:9123\"", config);
        }

        [Test]
        public void UpsertHttpMcpServer_ReplacesExistingCodexMcpServerSection()
        {
            var path = CreateTempConfig(
                "[mcp_servers.oddb]\n" +
                "url = \"http://127.0.0.1:9123\"\n" +
                "enabled = false\n" +
                "\n" +
                "[mcp_servers.other]\n" +
                "url = \"http://localhost:3000/mcp\"\n");

            TomlConfigFile.UpsertHttpMcpServer(path, "oddb", "http://127.0.0.1:9130");

            var config = File.ReadAllText(path);
            Assert.That(CountOccurrences(config, "[mcp_servers.oddb]"), Is.EqualTo(1));
            StringAssert.Contains("url = \"http://127.0.0.1:9130\"", config);
            StringAssert.DoesNotContain("enabled = false", config);
            StringAssert.Contains("[mcp_servers.other]", config);
        }

        [Test]
        public void RemoveMcpServer_RemovesCodexMcpServerAndLeavesOtherServers()
        {
            var path = CreateTempConfig(
                "[mcp_servers.oddb]\n" +
                "url = \"http://127.0.0.1:9123\"\n" +
                "\n" +
                "[mcp_servers.other]\n" +
                "url = \"http://localhost:3000/mcp\"\n");

            var removed = TomlConfigFile.RemoveMcpServer(path, "oddb");

            var config = File.ReadAllText(path);
            Assert.That(removed, Is.True);
            StringAssert.DoesNotContain("[mcp_servers.oddb]", config);
            StringAssert.Contains("[mcp_servers.other]", config);
        }

        [Test]
        public void RemoveMcpServer_StopsAtArrayTables()
        {
            var path = CreateTempConfig(
                "[mcp_servers.oddb]\n" +
                "url = \"http://127.0.0.1:9123\"\n" +
                "\n" +
                "[[profiles]]\n" +
                "name = \"default\"\n");

            TomlConfigFile.RemoveMcpServer(path, "oddb");

            var config = File.ReadAllText(path);
            StringAssert.DoesNotContain("[mcp_servers.oddb]", config);
            StringAssert.Contains("[[profiles]]", config);
            StringAssert.Contains("name = \"default\"", config);
        }

        private static string CreateTempConfig(string contents)
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "config.toml");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, contents);
            return path;
        }

        private static int CountOccurrences(string text, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }
    }
}
