using System;
using NUnit.Framework;
using TeamODD.ODDB.Editors;
using TeamODD.ODDB.Editors.Commands;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBEditorSessionTests
    {
        [Test]
        public void Current_SharesRuntimeUseCaseAndDatabase()
        {
            var session = ODDBEditorSession.Current;

            Assert.That(ODDBEditorSession.Current, Is.SameAs(session));
            Assert.That(session.Commands, Is.SameAs(ODDBEditorRuntime.UseCase));
            Assert.That(session.Database, Is.SameAs(ODDBEditorRuntime.Database));
            Assert.That(session.DatabasePath, Is.Not.Empty);
        }

        [Test]
        public void MissingTable_ReadHelpersReturnFalseOrThrowClearly()
        {
            var session = ODDBEditorSession.Current;
            const string missingTableId = "__oddb_missing_table__";

            Assert.That(session.TryGetTable(missingTableId, out _), Is.False);
            Assert.That(session.TryGetFieldIndex(missingTableId, "Attack", out _), Is.False);
            Assert.That(session.TryGetCell(missingTableId, "sword", "Attack", out _), Is.False);
            Assert.That(session.TryGetValue<int>(missingTableId, "sword", "Attack", out _), Is.False);
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
                () => session.GetTable(missingTableId));
        }

        [Test]
        public void MarkSaved_NotifiesSessionStateListeners()
        {
            var processor = new CommandProcessor();
            var notifications = 0;
            processor.OnHistoryChanged += () => notifications++;

            processor.Execute(new StubCommand());
            processor.MarkSaved();

            Assert.That(processor.IsDirty, Is.False);
            Assert.That(notifications, Is.EqualTo(2));
        }

        private sealed class StubCommand : ICommand
        {
            public void Execute() { }
            public void Undo() { }
            public string Name => "Stub";
            public DateTime ExecutionTime { get; set; }
        }
    }
}
