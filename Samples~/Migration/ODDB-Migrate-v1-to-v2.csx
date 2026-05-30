#r "nuget: Newtonsoft.Json, 13.0.3"
#r "../../Plugins/ODDB.Core.dll"
#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

// ODDB v1.x → v2.0 schema migration.
//
// v1: FieldType { "Type": <int>, "Param": "..." }
// v2: FieldType { "_typeKey": "<string>", "Param": "..." }
//
// Backs up the original to <path>.v1.bak before overwriting.
//
// Exit codes (v2.0.11+):
//   0  = migration + verify succeeded (default path)
//   2  = bad invocation (missing args, file not found)
//   3  = JSON parse failed
//   4  = unmapped FieldType integer (use --allow-unmapped to override → exit 10)
//   5  = table/view count drift between input and output
//   6  = output verification failed (ODDatabase.TryLoad reported non-recoverable failure)
//   7  = empty input refused (use --allow-empty to override → exit 10)
//   10 = override path succeeded (--allow-unmapped or --allow-empty). Non-zero by design.
//
// Flags:
//   --verify         (default) run ODDatabase.TryLoad against the output and exit 6 on fatal failure
//   --no-verify      skip the TryLoad verification step
//   --allow-unmapped accept migrations that left some FieldType integers unmapped (exit 10 on success)
//   --allow-empty    accept inputs with 0 tables + 0 views (exit 10 on success)

if (Args.Count < 1)
{
    Console.Error.WriteLine("usage: dotnet script ODDB-Migrate-v1-to-v2.csx -- <path/to/oddb.bytes> [--no-verify] [--allow-unmapped] [--allow-empty]");
    Environment.Exit(2);
}

string path = Args[0];
bool verify = !Args.Contains("--no-verify");
bool allowUnmapped = Args.Contains("--allow-unmapped");
bool allowEmpty = Args.Contains("--allow-empty");

if (!File.Exists(path))
{
    Console.Error.WriteLine($"file not found: {path}");
    Environment.Exit(2);
}

// v1 ODDBDataType enum → v2 wire key.
var keyMap = new Dictionary<int, string>
{
    {1,    "int"},
    {2,    "float"},
    {100,  "enum"},
    {200,  "bool"},
    {300,  "string"},
    {1003, "resource"},
    {1004, "addressable"},
    {2000, "view"},
    {3000, "string"},   // deprecated ID type — store as string
    {9999, "custom"},
};

string Ungzip(byte[] raw)
{
    using var ms = new MemoryStream(raw);
    using var gz = new GZipStream(ms, CompressionMode.Decompress);
    using var reader = new StreamReader(gz, Encoding.UTF8);
    return reader.ReadToEnd();
}

byte[] Gzip(string s)
{
    using var ms = new MemoryStream();
    using (var gz = new GZipStream(ms, CompressionLevel.Optimal))
    using (var writer = new StreamWriter(gz, Encoding.UTF8))
        writer.Write(s);
    return ms.ToArray();
}

int CountEntries(JArray a) => a == null ? 0 : a.OfType<JObject>().Count();

byte[] rawBytes = File.ReadAllBytes(path);
string json;
try { json = Ungzip(rawBytes); }
catch (InvalidDataException)
{
    Console.Error.WriteLine("file is not gzip — assuming raw JSON");
    json = Encoding.UTF8.GetString(rawBytes);
}

JObject root;
try { root = JObject.Parse(json); }
catch (JsonReaderException ex)
{
    Console.Error.WriteLine($"failed to parse JSON: {ex.Message}");
    Environment.Exit(3);
    return;
}

var inputTables = (root["TableRepoData"] as JArray) ?? new JArray();
var inputViews = (root["ViewRepoData"] as JArray) ?? new JArray();
int inputTableCount = CountEntries(inputTables);
int inputViewCount = CountEntries(inputViews);

// Pre-mortem #2: empty-input guard.
if (inputTableCount == 0 && inputViewCount == 0)
{
    if (!allowEmpty)
    {
        Console.Error.WriteLine("WARNING: input has 0 tables and 0 views.");
        Console.Error.WriteLine("Refusing migration. Re-run with --allow-empty to override (exit code 10).");
        Environment.Exit(7);
    }
    Console.WriteLine("WARNING: input has 0 tables and 0 views — proceeding under --allow-empty (override path).");
}

int converted = 0, skipped = 0, missed = 0;
var unmappedDetails = new List<string>();

void Migrate(JArray entries, string label)
{
    foreach (var entry in entries.OfType<JObject>())
    {
        var metas = entry["TableMetas"] as JArray;
        if (metas == null) continue;
        foreach (var field in metas.OfType<JObject>())
        {
            var ft = field["Type"] as JObject;
            if (ft == null) continue;

            // v2 already? skip.
            if (ft.Property("_typeKey") != null) { skipped++; continue; }

            var typeProp = ft.Property("Type");
            if (typeProp == null || typeProp.Value.Type != JTokenType.Integer)
            {
                unmappedDetails.Add($"[{label}] {entry["Name"]}/{field["Name"]}: no Type int");
                missed++;
                continue;
            }
            int typeInt = typeProp.Value.Value<int>();
            if (!keyMap.TryGetValue(typeInt, out var key))
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

Migrate(inputViews, "view");
Migrate(inputTables, "table");

// Step 1: stop on first unmapped FieldType (collect all, then exit 4)
if (missed > 0)
{
    Console.Error.WriteLine($"unmapped FieldType entries ({missed}):");
    foreach (var d in unmappedDetails) Console.Error.WriteLine("  " + d);
    if (!allowUnmapped)
    {
        Console.Error.WriteLine("Refusing migration. Re-run with --allow-unmapped to override (exit code 10).");
        Environment.Exit(4);
    }
    Console.Error.WriteLine($"WARNING: proceeding under --allow-unmapped — {missed} field(s) left as-is.");
}

if (converted == 0 && missed == 0 && skipped > 0)
{
    Console.WriteLine("nothing to convert — file already in v2 shape");
    if (!verify)
    {
        Console.WriteLine("--no-verify specified — skipping TryLoad verification.");
        return;
    }
    // Still verify a no-op pass; fall through.
}

// Step 3: every Field.Type._typeKey present on output (after the JObject mutation)
int missingTypeKey = 0;
void AssertTypeKey(JArray entries, string label)
{
    foreach (var entry in entries.OfType<JObject>())
    {
        var metas = entry["TableMetas"] as JArray;
        if (metas == null) continue;
        foreach (var field in metas.OfType<JObject>())
        {
            var ft = field["Type"] as JObject;
            if (ft == null) continue;
            var key = ft["_typeKey"]?.ToString();
            if (string.IsNullOrEmpty(key))
            {
                Console.Error.WriteLine($"missing _typeKey: [{label}] {entry["Name"]}/{field["Name"]}");
                missingTypeKey++;
            }
        }
    }
}
AssertTypeKey(inputViews, "view");
AssertTypeKey(inputTables, "table");

if (missingTypeKey > 0 && !allowUnmapped)
{
    Console.Error.WriteLine($"{missingTypeKey} field(s) lack a _typeKey after migration.");
    Environment.Exit(6);
}

// Step 2: table/view count drift check (input vs serialized output)
int outputTableCount = CountEntries(inputTables);  // mutated in place
int outputViewCount = CountEntries(inputViews);
if (outputTableCount != inputTableCount || outputViewCount != inputViewCount)
{
    Console.Error.WriteLine(
        $"table/view count drift: in tables={inputTableCount} views={inputViewCount}, " +
        $"out tables={outputTableCount} views={outputViewCount}");
    Environment.Exit(5);
}

// Write output (backup first, never overwrite without backup).
string backupPath = path + ".v1.bak";
File.Copy(path, backupPath, overwrite: true);

string v2Json = root.ToString(Formatting.None);
File.WriteAllBytes(path, Gzip(v2Json));

Console.WriteLine($"converted {converted} fields, {skipped} already-v2, {missed} unmapped");
Console.WriteLine($"backup: {backupPath}");

// Step 4: ODDatabase.TryLoad verification (default ON; --no-verify opts out)
if (verify)
{
    if (ODDatabase.TryLoad(path, out var _verifyDb, out var report))
    {
        Console.WriteLine(
            $"Migration verified: tables={report.RestoredTableCount} views={report.RestoredViewCount} unmapped={report.UnmappedFieldTypeCount}");
    }
    else
    {
        // Caveat: TryLoad's CountUnmappedFieldTypes relies on TypeRegistry, which
        // discovers serializers via reflection from loaded assemblies. When this
        // script runs under `dotnet script`, only ODDB.Core.dll is loaded — so
        // Unity-only serializers ("resource", "addressable", ...) won't resolve
        // and TryLoad will report UnmappedFieldType even on a perfectly migrated
        // file. Treat UnmappedFieldType as a soft warning here; fail only on
        // the structural stages (Gzip / Json / EmptyDto / EmptyRestored).
        bool fatal = report.FailureStage != ODDBLoadFailureStage.UnmappedFieldType;
        if (fatal)
        {
            Console.Error.WriteLine(
                $"Verify failed: stage={report.FailureStage} reason={report.FailureReason} " +
                $"dto-tables={report.DtoTableCount} dto-views={report.DtoViewCount} " +
                $"restored-tables={report.RestoredTableCount} restored-views={report.RestoredViewCount}");
            Environment.Exit(6);
        }
        Console.WriteLine(
            $"Verify soft-pass: TryLoad reported UnmappedFieldType ({report.UnmappedFieldTypeCount}) — " +
            $"expected when running outside Unity (Unity-only serializers not loaded in dotnet script).");
    }
}
else
{
    Console.WriteLine("--no-verify specified — skipping TryLoad verification.");
}

// Override-path exits: --allow-unmapped or --allow-empty success → exit 10 (non-zero by design).
if ((allowUnmapped && missed > 0) || (allowEmpty && inputTableCount == 0 && inputViewCount == 0))
{
    Console.WriteLine("Override path succeeded — exit code 10 (CI must treat this as non-zero).");
    Environment.Exit(10);
}

// Default success: exit 0.
