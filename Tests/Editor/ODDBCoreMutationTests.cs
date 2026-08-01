using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Async;
using TeamODD.ODDB.Runtime.Mutations;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBCoreMutationTests
    {
        [Test]
        public void RekeyRepositoryItem_PreservesOrder()
        {
            var database = new ODDatabase();
            database.Views.Create(new ODDBID("a"));
            database.Views.Create(new ODDBID("b"));
            database.Views.Create(new ODDBID("c"));

            var changed = ODDBMutations.RekeyRepositoryItem(
                database.Views,
                new ODDBID("b"),
                new ODDBID("renamed"));

            Assert.That(changed, Is.True);
            Assert.That(
                database.Views.GetAll().Select(view => view.ID.ToString()),
                Is.EqualTo(new[] { "a", "renamed", "c" }));
        }

        [Test]
        public void RekeyRow_PreservesOrder()
        {
            var table = new Table();
            table.AddRow(new ODDBID("a"));
            table.AddRow(new ODDBID("b"));
            table.AddRow(new ODDBID("c"));

            var changed = table.RekeyRow("b", "renamed");

            Assert.That(changed, Is.True);
            Assert.That(
                table.Rows.Select(row => row.ID.ToString()),
                Is.EqualTo(new[] { "a", "renamed", "c" }));
        }

        [Test]
        public void FieldAndCellMutations_UseCoreApis()
        {
            var table = new Table();
            table.ID = new ODDBID("table");
            table.AddField(new Field("Value", new FieldType("string")));
            table.AddRow(new ODDBID("row"));

            Assert.That(table.SetCellData("row", 0, "42", direct: true), Is.True);
            Assert.That(table.GetCell("row", 0).SerializedData, Is.EqualTo("42"));

            Assert.That(table.SetFieldType(0, "int", string.Empty), Is.True);
            Assert.That(table.TotalFields[0].Type.TypeKey, Is.EqualTo("int"));
        }

        [Test]
        public void MoveViewSibling_UsesSiblingIndicesAndPreservesOtherOrder()
        {
            var database = new ODDatabase();
            database.Views.Create(new ODDBID("a"));
            database.Views.Create(new ODDBID("b"));
            database.Views.Create(new ODDBID("c"));

            var changed = ODDBMutations.MoveViewSibling(database, "b", 1, 0);

            Assert.That(changed, Is.True);
            Assert.That(
                database.Views.GetAll().Select(view => view.ID.ToString()),
                Is.EqualTo(new[] { "b", "a", "c" }));
        }

        [Test]
        public void ResetRuntimeState_ClearsPendingCallbacksButPreservesAsyncLoader()
        {
            var loader = new StubAsyncLoader();
            TeamODD.ODDB.Runtime.ODDB.RegisterAsyncLoader(loader, replaceExisting: true);
            ODDBConverter.OnDatabaseCreated.Add(new DataBaseCreateEvent());

            TeamODD.ODDB.Runtime.ODDB.ResetRuntimeState();

            Assert.That(ODDBConverter.OnDatabaseCreated, Is.Empty);
            Assert.That(TeamODD.ODDB.Runtime.ODDB.HasAsyncLoader, Is.True);
            Assert.That(TeamODD.ODDB.Runtime.ODDB.UnregisterAsyncLoader(loader), Is.True);
        }

        [Test]
        public void TryRegisterAsyncLoader_DoesNotReplaceExistingLoader()
        {
            var first = new StubAsyncLoader();
            var second = new StubAsyncLoader();
            TeamODD.ODDB.Runtime.ODDB.RegisterAsyncLoader(first, replaceExisting: true);

            Assert.That(TeamODD.ODDB.Runtime.ODDB.TryRegisterAsyncLoader(second), Is.False);
            Assert.That(TeamODD.ODDB.Runtime.ODDB.UnregisterAsyncLoader(first), Is.True);
        }

        private sealed class StubAsyncLoader : IAsyncLoader
        {
            public System.Threading.Tasks.Task<T> GetAsync<T>(
                string key,
                System.Threading.CancellationToken cancellationToken = default)
                => System.Threading.Tasks.Task.FromResult(default(T));

            public void Release<T>(T asset)
            {
            }
        }
    }
}
