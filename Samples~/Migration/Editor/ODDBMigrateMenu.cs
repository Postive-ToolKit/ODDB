using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Samples.Migration
{
    /// <summary>
    /// Unity Editor menu wrapper around the v1→v2 migration. Equivalent to
    /// running `Samples~/Migration/ODDB-Migrate-v1-to-v2.csx` via `dotnet script`,
    /// but available as a one-click menu after importing the sample.
    /// </summary>
    public static class ODDBMigrateMenu
    {
        private const string MENU_ROOT = "ODDB/Migrate v1 → v2/";

        // Identical wire-key map to ODDB-Migrate-v1-to-v2.csx (kept in sync).
        private static readonly Dictionary<int, string> KeyMap = new Dictionary<int, string>
        {
            { 1,    "int" },
            { 2,    "float" },
            { 100,  "enum" },
            { 200,  "bool" },
            { 300,  "string" },
            { 1003, "resource" },
            { 1004, "addressable" },
            { 2000, "view" },
            { 3000, "string" },
            { 9999, "custom" },
        };

        [MenuItem(MENU_ROOT + "Pick File...")]
        private static void PickAndMigrate()
        {
            var initial = ResolveInitialDirectory();
            var picked = EditorUtility.OpenFilePanel(
                "Select ODDB v1 .bytes file to migrate",
                initial,
                "bytes");
            if (string.IsNullOrEmpty(picked)) return;
            RunMigrationInteractive(picked);
        }

        [MenuItem(MENU_ROOT + "Current Database")]
        private static void MigrateCurrent()
        {
            string path;
            try { path = ODDBRuntimeSettings.ResolveDatabasePath(); }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "ODDB Migration",
                    "Could not resolve the current database path:\n" + e.Message,
                    "OK");
                return;
            }
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog(
                    "ODDB Migration",
                    "Current database file does not exist:\n" + path,
                    "OK");
                return;
            }
            RunMigrationInteractive(path);
        }

        private static string ResolveInitialDirectory()
        {
            try
            {
                var current = ODDBRuntimeSettings.ResolveDatabasePath();
                var dir = Path.GetDirectoryName(current);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) return dir;
            }
            catch { /* fall through */ }
            return Application.dataPath;
        }

        private static void RunMigrationInteractive(string path)
        {
            if (!EditorUtility.DisplayDialog(
                    "ODDB Migration — Confirm",
                    $"Migrate this file to v2 in-place?\n\n{path}\n\nA backup will be written to:\n{path}.v1.bak",
                    "Migrate",
                    "Cancel"))
            {
                return;
            }

            try
            {
                var result = RunMigration(path);
                EditorUtility.DisplayDialog(
                    "ODDB Migration — Result",
                    result.ToHumanReadable(),
                    "OK");
                if (result.Wrote)
                    AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ODDB] Migration failed: {e}");
                EditorUtility.DisplayDialog(
                    "ODDB Migration — Failed",
                    $"Migration aborted:\n{e.Message}",
                    "OK");
            }
        }

        // ── core migration ────────────────────────────────────────────────────

        private struct MigrationResult
        {
            public bool Wrote;
            public int Converted;
            public int Skipped;
            public int Missed;
            public string BackupPath;
            public string VerifyStage;
            public string VerifyReason;
            public int VerifyUnmappedCount;
            public int RestoredTables;
            public int RestoredViews;

            public string ToHumanReadable()
            {
                var sb = new StringBuilder();
                if (!Wrote && Skipped > 0 && Converted == 0 && Missed == 0)
                    sb.AppendLine("Nothing to convert — file already in v2 shape.");
                else
                    sb.AppendLine($"Converted {Converted} field(s); already-v2 {Skipped}; unmapped {Missed}.");

                if (Wrote)
                    sb.AppendLine($"Backup: {BackupPath}");

                if (!string.IsNullOrEmpty(VerifyStage))
                    sb.AppendLine($"\nVerify: FAILED ({VerifyStage}) — {VerifyReason}");
                else
                    sb.AppendLine($"\nVerify: OK — tables={RestoredTables} views={RestoredViews} unmapped={VerifyUnmappedCount}");

                return sb.ToString();
            }
        }

        private static MigrationResult RunMigration(string path)
        {
            var raw = File.ReadAllBytes(path);

            string json;
            try { json = Ungzip(raw); }
            catch (InvalidDataException)
            {
                // Not a gzip file — assume raw JSON.
                json = Encoding.UTF8.GetString(raw);
            }
            // Defensive: tolerate any pre-existing BOM at the head before parsing.
            if (json.Length > 0 && json[0] == '﻿')
                json = json.Substring(1);

            JObject root;
            try { root = JObject.Parse(json); }
            catch (Exception e)
            {
                throw new InvalidDataException("JSON parse failed: " + e.Message, e);
            }

            int converted = 0, skipped = 0, missed = 0;
            var unmappedDetails = new List<string>();

            void Migrate(JArray entries, string label)
            {
                if (entries == null) return;
                foreach (var entry in entries.OfType<JObject>())
                {
                    var metas = entry["TableMetas"] as JArray;
                    if (metas == null) continue;
                    foreach (var field in metas.OfType<JObject>())
                    {
                        var ft = field["Type"] as JObject;
                        if (ft == null) continue;
                        if (ft.Property("_typeKey") != null) { skipped++; continue; }
                        var typeProp = ft.Property("Type");
                        if (typeProp == null || typeProp.Value.Type != JTokenType.Integer)
                        {
                            unmappedDetails.Add($"[{label}] {entry["Name"]}/{field["Name"]}: no Type int");
                            missed++;
                            continue;
                        }
                        int typeInt = typeProp.Value.Value<int>();
                        if (!KeyMap.TryGetValue(typeInt, out var key))
                        {
                            unmappedDetails.Add($"[{label}] {entry["Name"]}/{field["Name"]}: unknown enum int {typeInt}");
                            missed++;
                            continue;
                        }
                        ft["_typeKey"] = key;
                        typeProp.Remove();
                        converted++;
                    }
                }
            }

            Migrate(root["ViewRepoData"] as JArray, "view");
            Migrate(root["TableRepoData"] as JArray, "table");

            if (missed > 0)
            {
                var detail = string.Join("\n  ", unmappedDetails);
                throw new InvalidOperationException(
                    $"{missed} field(s) had unmapped FieldType integers — refusing to write a partial migration:\n  {detail}");
            }

            var result = new MigrationResult
            {
                Converted = converted,
                Skipped = skipped,
                Missed = missed,
            };

            // Nothing to convert AND nothing already-v2 AND nothing missed — empty file?
            // Already-v2 only — no write needed, run verify.
            if (converted == 0 && missed == 0 && skipped > 0)
            {
                VerifyAndPopulate(path, ref result);
                return result;
            }

            var backupPath = path + ".v1.bak";
            File.Copy(path, backupPath, overwrite: true);
            result.BackupPath = backupPath;

            var v2Json = root.ToString(Newtonsoft.Json.Formatting.None);
            File.WriteAllBytes(path, GzipNoBom(v2Json));
            result.Wrote = true;

            VerifyAndPopulate(path, ref result);
            return result;
        }

        private static void VerifyAndPopulate(string path, ref MigrationResult result)
        {
            if (ODDatabase.TryLoad(path, out _, out var report))
            {
                result.RestoredTables = report.RestoredTableCount;
                result.RestoredViews = report.RestoredViewCount;
                result.VerifyUnmappedCount = report.UnmappedFieldTypeCount;
            }
            else
            {
                result.VerifyStage = report.FailureStage;
                result.VerifyReason = report.FailureReason;
                result.RestoredTables = report.RestoredTableCount;
                result.RestoredViews = report.RestoredViewCount;
                result.VerifyUnmappedCount = report.UnmappedFieldTypeCount;
            }
        }

        // ── gzip helpers ──────────────────────────────────────────────────────

        private static string Ungzip(byte[] raw)
        {
            using var ms = new MemoryStream(raw);
            using var gz = new GZipStream(ms, CompressionMode.Decompress);
            using var reader = new StreamReader(gz, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static byte[] GzipNoBom(string s)
        {
            using var ms = new MemoryStream();
            // System.IO.Compression.CompressionLevel — fully qualified because
            // UnityEngine.CompressionLevel exists and would otherwise be ambiguous.
            using (var gz = new GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal))
            // new UTF8Encoding(false) → no BOM prepended; matches the BOM fix
            // applied to ODDB-Migrate-v1-to-v2.csx in v2.0.12.
            using (var writer = new StreamWriter(gz, new UTF8Encoding(false)))
                writer.Write(s);
            return ms.ToArray();
        }
    }
}
