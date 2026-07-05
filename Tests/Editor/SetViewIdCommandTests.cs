using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Commands;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class SetViewIdCommandTests
    {
        [Test]
        public void Execute_RekeysViewPreservesOrderAndNotifiesOldAndNewIds()
        {
            var repository = new ViewRepository<View>();
            repository.Create(new ODDBID("first"));
            repository.Create(new ODDBID("second"));
            var notifiedIds = new System.Collections.Generic.List<string>();

            var command = new SetViewIdCommand(
                repository,
                new ODDBID("first"),
                new ODDBID("renamed"),
                id => notifiedIds.Add(id));

            command.Execute();

            Assert.That(repository.Read(new ODDBID("first")), Is.Null);
            Assert.That(repository.Read(new ODDBID("renamed")), Is.Not.Null);
            var ids = repository.GetAll().Select(view => view.ID.ToString()).ToArray();
            Assert.That(ids, Is.EqualTo(new[] { "renamed", "second" }));
            Assert.That(notifiedIds, Is.EqualTo(new[] { "first", "renamed" }));
        }

        [Test]
        public void Undo_RestoresOldIdPreservesOrderAndNotifiesNewAndOldIds()
        {
            var repository = new ViewRepository<View>();
            repository.Create(new ODDBID("first"));
            repository.Create(new ODDBID("second"));
            var notifiedIds = new System.Collections.Generic.List<string>();

            var command = new SetViewIdCommand(
                repository,
                new ODDBID("first"),
                new ODDBID("renamed"),
                id => notifiedIds.Add(id));

            command.Execute();
            notifiedIds.Clear();
            command.Undo();

            Assert.That(repository.Read(new ODDBID("renamed")), Is.Null);
            Assert.That(repository.Read(new ODDBID("first")), Is.Not.Null);
            var ids = repository.GetAll().Select(view => view.ID.ToString()).ToArray();
            Assert.That(ids, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(notifiedIds, Is.EqualTo(new[] { "renamed", "first" }));
        }
    }
}
