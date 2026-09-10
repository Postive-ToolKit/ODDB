using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Utils.Converters;

var failures = 0;
void Check(bool condition, string name)
{
    if (condition) return;
    failures++;
    Console.Error.WriteLine(name);
}
var db = ODDatabase.CreateEmpty();
var table = (Table)db.Tables.Create(new ODDBID("items"));
table.AddField(new Field("Name", new FieldType("string", "")));
table.AddRow(new ODDBID("sword"));
table.SetCellData("sword", 0, "Sword", true);
var bytes = new ODDBConverter().Export(db);
Check(ODDatabase.TryLoadBytes(bytes, out var loaded, out var report), "bytes load");
Check(report.IsSafeToSave && ((Table)loaded.Tables.Read(new ODDBID("items"))).GetCell("sword", 0).SerializedData == "Sword", "rows hydrated");
foreach (var invalid in new[] { (byte[])null, Array.Empty<byte>(), new byte[] { 1, 2, 3 }, bytes.Take(10).ToArray() })
    Check(!ODDatabase.TryLoadBytes(invalid, out _, out var badReport) && !badReport.IsSafeToSave, "invalid bytes reject");
Check(!ODDatabase.TryLoadBytes(new ODDBConverter().Export(ODDatabase.CreateEmpty()), out _, out var emptyReport)
    && emptyReport.FailureStage == ODDBLoadFailureStage.EmptyDtoOnExistingFile, "empty-file guard retained");
var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
    ODDatabase.TryLoadBytes(bytes, out var parallel, out var parallelReport) && parallelReport.IsSafeToSave &&
    ((Table)parallel.Tables.Read(new ODDBID("items"))).Rows.Count == 1)).ToArray();
Task.WaitAll(tasks);
Check(tasks.All(task => task.Result), "concurrent safe loads isolate hydration callbacks");
var path = Path.Combine(Path.GetTempPath(), "oddb-core-check-" + Guid.NewGuid() + ".bytes");
try
{
    File.WriteAllBytes(path, bytes);
    Check(ODDatabase.TryLoad(path, out var fromFile, out var fileReport) && fileReport.FileSize == bytes.Length && fromFile.Count == loaded.Count, "file and bytes parity");
}
finally { File.Delete(path); }
Check(ODDBConverter.OnDatabaseCreated.Count == 0, "callbacks drained");

var unresolvedDatabase = ODDatabase.CreateEmpty();
var unresolvedTable = (Table)unresolvedDatabase.Tables.Create(new ODDBID("unresolved-items"));
unresolvedTable.UnresolvedBindType = "ODDB.Generated.MissingItem";
unresolvedTable.AddField(new Field("Name", new FieldType("string")));
unresolvedTable.AddRow(new ODDBID("unresolved-row"));
var unresolvedBytes = new ODDBConverter().Export(unresolvedDatabase);
Check(ODDatabase.TryLoadBytes(unresolvedBytes, out var unresolvedLoaded, out var unresolvedReport) &&
    unresolvedReport.IsSafeToSave, "unknown binding database remains loadable");
if (unresolvedLoaded != null)
{
    var unresolvedLoadedTable = (Table)unresolvedLoaded.Tables.Read(new ODDBID("unresolved-items"));
    Check(unresolvedLoadedTable.BindType == null &&
        unresolvedLoadedTable.UnresolvedBindType == "ODDB.Generated.MissingItem",
        "unknown binding is retained after load");
    var unresolvedRoundTrip = new ODDBConverter().Export(unresolvedLoaded);
    Check(ODDatabase.TryLoadBytes(unresolvedRoundTrip, out var unresolvedReloaded, out _) &&
        ((Table)unresolvedReloaded.Tables.Read(new ODDBID("unresolved-items"))).UnresolvedBindType ==
        "ODDB.Generated.MissingItem", "unknown binding survives save and reload");
}

var opaqueDatabase = ODDatabase.CreateEmpty();
var opaqueTable = (Table)opaqueDatabase.Tables.Create(new ODDBID("opaque-items"));
opaqueTable.AddField(new Field("HugeValue", new FieldType("bigdouble")));
opaqueTable.AddRow(new ODDBID("large"));
opaqueTable.SetCellData("large", 0, "1e1000000", true);
var opaqueBytes = new ODDBConverter().Export(opaqueDatabase);
Check(ODDatabase.TryLoadBytes(opaqueBytes, out var opaqueLoaded, out var opaqueReport) &&
    opaqueReport.IsSafeToSave && opaqueReport.UnmappedFieldTypeCount == 1,
    "unknown v2 field type remains safely loadable");
if (opaqueLoaded != null)
{
    var opaqueLoadedTable = (Table)opaqueLoaded.Tables.Read(new ODDBID("opaque-items"));
    Check(opaqueLoadedTable.GetCell("large", 0).FieldType.TypeKey == "bigdouble" &&
        opaqueLoadedTable.GetCell("large", 0).SerializedData == "1e1000000",
        "unknown field key and raw value are retained");
    var opaqueRoundTrip = new ODDBConverter().Export(opaqueLoaded);
    Check(ODDatabase.TryLoadBytes(opaqueRoundTrip, out var opaqueReloaded, out _) &&
        ((Table)opaqueReloaded.Tables.Read(new ODDBID("opaque-items"))).GetCell("large", 0).SerializedData == "1e1000000",
        "unknown field survives save and reload");
}

var portDatabase = ODDatabase.CreateEmpty();
var portTable = (Table)portDatabase.Tables.Create(new ODDBID("port-items"));
portTable.BindType = typeof(PortItem);
portTable.AddField(new Field("Name", new FieldType("string")));
for (var i = 0; i < 7; i++)
{
    var rowId = $"item_{i:000}";
    portTable.AddRow(new ODDBID(rowId));
    portTable.SetCellData(rowId, 0, "Item " + i, true);
}
var linkTable = (Table)portDatabase.Tables.Create(new ODDBID("port-links"));
linkTable.BindType = typeof(PortLink);
linkTable.AddField(new Field("Target", new FieldType("view")));
linkTable.AddRow(new ODDBID("link"));
linkTable.GetCell("link", 0).SetData("item_003", true);

using var incremental = portDatabase.BeginPortData();
Check(incremental.TotalRows == 8 && incremental.ProcessedRows == 0 && !portDatabase.IsPorted,
    "incremental operation starts unported");
var firstBatch = incremental.Step(2);
Check(firstBatch == 2 && incremental.ProcessedRows == 2 && !incremental.IsCompleted && !portDatabase.IsPorted,
    "incremental operation bounds first batch");
while (!incremental.IsCompleted)
{
    var batch = incremental.Step(2);
    Check(batch <= 2, "incremental operation respects every batch budget");
}
Check(portDatabase.IsPorted && incremental.Progress == 1f && portDatabase.GetEntity<PortItem>("item_003")?.Name == "Item 3",
    "incremental operation completes entities");
Check(portDatabase.GetEntity<PortLink>("link")?.Target?.ID == "item_003",
    "incremental operation resolves lazy view references after completion");

var canceledDatabase = ODDatabase.CreateEmpty();
var canceledTable = (Table)canceledDatabase.Tables.Create(new ODDBID("cancel-items"));
canceledTable.BindType = typeof(PortItem);
canceledTable.AddField(new Field("Name", new FieldType("string")));
for (var i = 0; i < 4; i++)
{
    var rowId = $"cancel_{i}";
    canceledTable.AddRow(new ODDBID(rowId));
    canceledTable.SetCellData(rowId, 0, rowId, true);
}
using (var canceled = canceledDatabase.BeginPortData())
{
    canceled.Step(1);
    canceled.Cancel();
    Check(canceled.IsCompleted && !canceledDatabase.IsPorted && !canceledDatabase.GetLiveEntityIds().Any(),
        "canceled incremental operation discards partial cache");
}
using (var retry = canceledDatabase.BeginPortData())
{
    retry.Complete();
    Check(canceledDatabase.IsPorted && canceledDatabase.GetEntity<PortItem>("cancel_3") != null,
        "canceled operation can be retried cleanly");
}
Console.WriteLine($"ODDB_CORE failures={failures}");
return failures == 0 ? 0 : 1;

sealed class PortItem : ODDBEntity
{
    public string Name = "";
}

sealed class PortLink : ODDBEntity
{
    public PortItem Target;
}
