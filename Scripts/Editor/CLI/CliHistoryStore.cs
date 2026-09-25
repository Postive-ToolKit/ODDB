using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TeamODD.ODDB.Editors.CLI
{
    /// <summary>Project-local audit of commands issued through the CLI.</summary>
    public static class CliHistoryStore
    {
        public static object Read(string projectPath)
        {
            var path = HistoryPath(projectPath);
            if (!File.Exists(path)) return new { entries = new JArray() };
            return new { entries = JArray.Parse(File.ReadAllText(path)) };
        }

        public static void Append(string projectPath, string operation, JToken arguments)
        {
            var path = HistoryPath(projectPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var entries = File.Exists(path) ? JArray.Parse(File.ReadAllText(path)) : new JArray();
            var target = new JObject();
            foreach (var key in new[] { "viewId", "tableId", "rowId", "fieldIndex" })
                if (arguments?[key] != null) target[key] = arguments[key].DeepClone();
            entries.Add(new JObject
            {
                ["operation"] = operation,
                ["target"] = target,
                ["executedUtc"] = DateTime.UtcNow
            });
            while (entries.Count > 100) entries.RemoveAt(0);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonConvert.SerializeObject(entries));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }

        private static string HistoryPath(string projectPath) =>
            Path.Combine(projectPath, "Library", "ODDB", "cli", "history.json");
    }
}
