using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Utils.Sheets;
using TeamODD.ODDB.Editors.MCP.ClientRegistration;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

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

    public sealed class GoogleSheetAppScriptFormatSafetyTests
    {
        [Test]
        public void ResourceScript_MergesManagedRowsWithoutClearingUploadRange()
        {
            var script = ReadScript();

            Assert.That(script, Does.Not.Match(@"\bsheet\.clear\s*\("));
            Assert.That(script, Does.Not.Match(@"\bsheet\.clearContents\s*\("));
            Assert.That(script, Does.Not.Match(@"\.clearContent\s*\("));
            StringAssert.DoesNotContain("clearUploadedRangeContents", script);
            StringAssert.Contains("applyOddbSheetData(sheet, values);", script);
            StringAssert.Contains("function applyOddbSheetData(sheet, values)", script);
        }

        [Test]
        public void ResourceScript_PreservesCommentRowsAndMarksRemovedManagedRows()
        {
            var script = ReadScript();

            StringAssert.Contains("function isCommentRow(firstCell)", script);
            StringAssert.Contains("markRemovedRows(sheet, existingRowsById, incomingRowsById, idColumn, managedColumns);", script);
            StringAssert.Contains("const REMOVED_MARKER = \"#REMOVED\";", script);
            StringAssert.Contains("clear({contentsOnly: true})", script);
        }

        [Test]
        public void ApplySheetToTable_SkipsRowsWhoseFirstCellStartsWithCommentPrefix()
        {
            var database = new ODDatabase();
            var table = (Table)database.Tables.Create(new ODDBID("item"));
            table.AddField(new Field("Name", new FieldType("string", string.Empty)));

            var sheet = new SheetInfo("ItemData", "item");
            sheet.Values.Add(new List<string> { "#NAME", "ID", "Name" });
            sheet.Values.Add(new List<string> { "#TYPE", "ID", "string" });
            sheet.Values.Add(new List<string> { "# designer note", "not-a-row", "Do not import this row" });
            sheet.Values.Add(new List<string> { string.Empty, "row1", "Iron Sword" });
            sheet.Values.Add(new List<string> { "#REMOVED", "old-row", "Old Value" });

            new ODDBSheetConverter(database).ApplySheetToTable(table, sheet);

            Assert.That(table.Rows.Count, Is.EqualTo(1));
            Assert.That(table.GetRow("row1"), Is.Not.Null);
            Assert.That(table.GetRow("not-a-row"), Is.Null);
            Assert.That(table.GetRow("old-row"), Is.Null);
        }

        private static string ReadScript()
        {
            return File.ReadAllText(Path.Combine(
                "Assets",
                "Plugins",
                "ODDB",
                "Resources",
                "GoogleSheetAppScript.txt"));
        }
    }
}
