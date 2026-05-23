#r "nuget: Newtonsoft.Json, 13.0.3"
#nullable disable

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ODDB v1.x → v2.0 schema migration.
//
// v1: FieldType { "Type": <int>, "Param": "..." }
// v2: FieldType { "_typeKey": "<string>", "Param": "..." }
//
// Backs up the original to <path>.v1.bak before overwriting.

if (Args.Count < 1)
{
    Console.Error.WriteLine("usage: dotnet script ODDB-Migrate-v1-to-v2.csx -- <path/to/oddb.bytes>");
    Environment.Exit(2);
}

string path = Args[0];
if (!File.Exists(path))
{
    Console.Error.WriteLine($"file not found: {path}");
    Environment.Exit(2);
}

// v1 ODDBDataType enum → v2 wire key.
var keyMap = new System.Collections.Generic.Dictionary<int, string>
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

int converted = 0, skipped = 0, missed = 0;

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
                Console.Error.WriteLine($"  [{label}] {entry["Name"]}/{field["Name"]}: no Type int — leaving as-is");
                missed++;
                continue;
            }
            int typeInt = typeProp.Value.Value<int>();
            if (!keyMap.TryGetValue(typeInt, out var key))
            {
                Console.Error.WriteLine($"  [{label}] {entry["Name"]}/{field["Name"]}: unknown enum int {typeInt} — leaving as-is");
                missed++;
                continue;
            }
            ft["_typeKey"] = key;
            typeProp.Remove();
            converted++;
        }
    }
}

Migrate((root["ViewRepoData"] as JArray) ?? new JArray(), "view");
Migrate((root["TableRepoData"] as JArray) ?? new JArray(), "table");

if (converted == 0 && missed == 0 && skipped > 0)
{
    Console.WriteLine("nothing to convert — file already in v2 shape");
    return;
}

string backupPath = path + ".v1.bak";
File.Copy(path, backupPath, overwrite: true);

string v2Json = root.ToString(Formatting.None);
File.WriteAllBytes(path, Gzip(v2Json));

Console.WriteLine($"converted {converted} fields, {skipped} already-v2, {missed} unmapped");
Console.WriteLine($"backup: {backupPath}");
