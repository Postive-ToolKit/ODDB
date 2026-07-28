using System;
using NUnit.Framework;
using TeamODD.ODDB.Editors;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBReloadAndImportTests
    {
        [Test]
        public void ImportStagingDatabase_IsIsolatedFromSourceDatabase()
        {
            var source = ODDatabase.CreateEmpty();
            var sourceTable = (Table)source.Tables.Create();
            sourceTable.AddRow(new ODDBID("source-row"));
            string stagingPath = null;

            try
            {
                var staging = ODDBEditorUseCase.CreateImportStagingDatabase(source, out stagingPath);
                var stagingTable = (Table)staging.Tables.Read(sourceTable.ID);

                stagingTable.Clear();

                Assert.That(sourceTable.Rows.Count, Is.EqualTo(1));
                Assert.That(stagingTable.Rows.Count, Is.Zero);
            }
            finally
            {
                ODDBEditorUseCase.TryDeleteImportStagingFile(stagingPath);
            }
        }

        [Test]
        public void ReloadDatabase_PreservesUseCaseIdentityAndSignalsFullReload()
        {
            var before = ODDBEditorRuntime.UseCase;
            var beforeDatabase = before.DataBase;
            string changedId = "not-called";
            Action<string> handler = id => changedId = id;
            before.OnViewChanged += handler;
            try
            {
                ODDBEditorRuntime.ReloadDatabase();

                Assert.That(ODDBEditorRuntime.UseCase, Is.SameAs(before));
                Assert.That(ODDBEditorRuntime.Database, Is.Not.SameAs(beforeDatabase));
                Assert.That(changedId, Is.Null);
            }
            finally
            {
                before.OnViewChanged -= handler;
            }
        }
    }
}
