using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Types;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBSharedCoreTests
    {
        [SetUp]
        public void SetUp() => TypeRegistry.ResetCache();

        [TearDown]
        public void TearDown() => TypeRegistry.ResetCache();

        [Test]
        public void TryLoadBytes_RestoresRowsLikeFileLoading()
        {
            var bytes = new ODDBConverter().Export(CreateItems());
            var path = Path.Combine(Path.GetTempPath(), "oddb-core-unity-" + Guid.NewGuid() + ".bytes");
            try
            {
                File.WriteAllBytes(path, bytes);
                Assert.That(ODDatabase.TryLoadBytes(bytes, out var loaded, out var report), Is.True);
                Assert.That(ODDatabase.TryLoad(path, out var fromFile, out var fileReport), Is.True);
                Assert.That(report.IsSafeToSave && fileReport.IsSafeToSave, Is.True);
                Assert.That(((Table)loaded.Tables.Read(new ODDBID("items"))).GetCell("item-1", 0).SerializedData,
                    Is.EqualTo("Item 1"));
                Assert.That(fromFile.ToDTO().TableRepoData.Count, Is.EqualTo(loaded.ToDTO().TableRepoData.Count));
                Assert.That(ODDBConverter.OnDatabaseCreated, Is.Empty);
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void TryLoadBytes_RejectsInvalidAndTruncatedPayloads()
        {
            var bytes = new ODDBConverter().Export(CreateItems());
            foreach (var invalid in new[] { null, Array.Empty<byte>(), new byte[] { 1, 2, 3 }, bytes.Take(10).ToArray() })
            {
                Assert.That(ODDatabase.TryLoadBytes(invalid, out _, out var report), Is.False);
                Assert.That(report.IsSafeToSave, Is.False);
            }
        }

        [Test]
        public void TryLoadBytes_RejectsUnexpectedEmptyDatabase()
        {
            var bytes = new ODDBConverter().Export(ODDatabase.CreateEmpty());
            Assert.That(ODDatabase.TryLoadBytes(bytes, out _, out var report), Is.False);
            Assert.That(report.IsSafeToSave, Is.False);
            Assert.That(report.FailureStage, Is.EqualTo(ODDBLoadFailureStage.EmptyDtoOnExistingFile));
        }

        [Test]
        public void UnknownBinding_SurvivesSaveAndReload()
        {
            var database = CreateItems();
            var table = (Table)database.Tables.Read(new ODDBID("items"));
            table.UnresolvedBindType = "ODDB.MissingAssembly.MissingEntity";
            for (var i = 0; i < 2; i++)
            {
                Assert.That(ODDatabase.TryLoadBytes(new ODDBConverter().Export(database), out database, out var report), Is.True);
                Assert.That(report.IsSafeToSave, Is.True);
                table = (Table)database.Tables.Read(new ODDBID("items"));
                Assert.That(table.BindType, Is.Null);
                Assert.That(table.UnresolvedBindType, Is.EqualTo("ODDB.MissingAssembly.MissingEntity"));
                Assert.That(table.Rows.Count, Is.EqualTo(3));
            }
        }

        [Test]
        public void UnknownV2Field_PreservesTypeParameterAndSerializedValue()
        {
            var database = ODDatabase.CreateEmpty();
            var table = (Table)database.Tables.Create(new ODDBID("opaque"));
            table.AddField(new Field("Value", new FieldType("__oddb_absent_core_serializer", "parameter")));
            table.AddRow(new ODDBID("row"));
            table.SetCellData("row", 0, "1e1000000", true);
            for (var i = 0; i < 2; i++)
            {
                Assert.That(ODDatabase.TryLoadBytes(new ODDBConverter().Export(database), out database, out var report), Is.True);
                Assert.That(report.IsSafeToSave, Is.True);
                Assert.That(report.UnmappedFieldTypeCount, Is.EqualTo(1));
                var cell = ((Table)database.Tables.Read(new ODDBID("opaque"))).GetCell("row", 0);
                Assert.That(cell.FieldType.TypeKey, Is.EqualTo("__oddb_absent_core_serializer"));
                Assert.That(cell.FieldType.Param, Is.EqualTo("parameter"));
                Assert.That(cell.SerializedData, Is.EqualTo("1e1000000"));
            }
        }

        [Test]
        public void IncrementalPort_RespectsBudgetAndCompletesEntities()
        {
            var database = CreateItems(bind: true);
            using var operation = database.BeginPortData();
            Assert.That(operation.TotalRows, Is.EqualTo(3));
            Assert.That(operation.Step(1), Is.EqualTo(1));
            Assert.That(database.IsPorted, Is.False);
            while (!operation.IsCompleted)
                Assert.That(operation.Step(1), Is.LessThanOrEqualTo(1));
            Assert.That(database.IsPorted, Is.True);
            Assert.That(operation.Progress, Is.EqualTo(1f));
            Assert.That(database.GetEntity<SharedCoreItem>("item-1").Name, Is.EqualTo("Item 1"));
        }

        [Test]
        public void IncrementalPort_CancelDiscardsPartialCacheAndAllowsRetry()
        {
            var database = CreateItems(bind: true);
            using (var operation = database.BeginPortData())
            {
                operation.Step(1);
                operation.Cancel();
                Assert.That(database.IsPorted, Is.False);
                Assert.That(database.GetLiveEntityIds(), Is.Empty);
            }
            using var retry = database.BeginPortData();
            retry.Complete();
            Assert.That(database.GetEntity<SharedCoreItem>("item-2").Name, Is.EqualTo("Item 2"));
        }

        [Test]
        public void SynchronousPort_RemainsCompatibleWithSharedCore()
        {
            var database = CreateItems(bind: true);
            database.PortData();
            Assert.That(database.IsPorted, Is.True);
            Assert.That(database.GetEntity<SharedCoreItem>("item-0").Name, Is.EqualTo("Item 0"));
        }

        private static ODDatabase CreateItems(bool bind = false)
        {
            var database = ODDatabase.CreateEmpty();
            var table = (Table)database.Tables.Create(new ODDBID("items"));
            if (bind) table.BindType = typeof(SharedCoreItem);
            table.AddField(new Field("Name", new FieldType("string")));
            for (var i = 0; i < 3; i++)
            {
                table.AddRow(new ODDBID("item-" + i));
                table.SetCellData("item-" + i, 0, "Item " + i, true);
            }
            return database;
        }

        public sealed class SharedCoreItem : ODDBEntity
        {
            public string Name;
        }
    }
}
