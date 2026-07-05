using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Commands;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class SetRowIdCommandTests
    {
        [Test]
        public void Execute_RekeysRowPreservesOrderAndNotifiesTable()
        {
            var database = new ODDatabase();
            var table = (Table)database.Tables.Create(new ODDBID("table"));
            table.AddRow(new ODDBID("first"));
            table.AddRow(new ODDBID("second"));
            var notifiedIds = new System.Collections.Generic.List<string>();

            var command = new SetRowIdCommand(
                table,
                "first",
                "renamed",
                id => notifiedIds.Add(id));

            command.Execute();

            Assert.That(table.GetRow("first"), Is.Null);
            Assert.That(table.GetRow("renamed"), Is.Not.Null);
            var ids = table.Rows.Select(row => row.ID.ToString()).ToArray();
            Assert.That(ids, Is.EqualTo(new[] { "renamed", "second" }));
            Assert.That(notifiedIds, Is.EqualTo(new[] { "table" }));
        }

        [Test]
        public void Undo_RestoresOldIdPreservesOrderAndNotifiesTable()
        {
            var database = new ODDatabase();
            var table = (Table)database.Tables.Create(new ODDBID("table"));
            table.AddRow(new ODDBID("first"));
            table.AddRow(new ODDBID("second"));
            var notifiedIds = new System.Collections.Generic.List<string>();

            var command = new SetRowIdCommand(
                table,
                "first",
                "renamed",
                id => notifiedIds.Add(id));

            command.Execute();
            notifiedIds.Clear();
            command.Undo();

            Assert.That(table.GetRow("renamed"), Is.Null);
            Assert.That(table.GetRow("first"), Is.Not.Null);
            var ids = table.Rows.Select(row => row.ID.ToString()).ToArray();
            Assert.That(ids, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(notifiedIds, Is.EqualTo(new[] { "table" }));
        }
    }
}
