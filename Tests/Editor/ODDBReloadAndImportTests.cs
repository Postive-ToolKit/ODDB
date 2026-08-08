using System;
using System.IO;
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

        [Test]
        public void RotateBackups_RemovesExpiredUnityMetaFiles()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "ODDB-BackupRotationTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var original = Path.Combine(directory, "ODDB.bytes");
                File.WriteAllText(original, "database");
                var backups = new string[3];
                for (var index = 0; index < backups.Length; index++)
                {
                    backups[index] = original + $".pre-save-20260808-00000{index}.bak";
                    File.WriteAllText(backups[index], "backup");
                    File.WriteAllText(backups[index] + ".meta", "meta");
                    File.SetLastWriteTimeUtc(backups[index], DateTime.UtcNow.AddMinutes(index));
                }

                ODDBBackup.RotateBackups(original, 1, ".pre-save-*.bak");

                Assert.That(File.Exists(backups[2]), Is.True);
                Assert.That(File.Exists(backups[2] + ".meta"), Is.True);
                for (var index = 0; index < 2; index++)
                {
                    Assert.That(File.Exists(backups[index]), Is.False);
                    Assert.That(File.Exists(backups[index] + ".meta"), Is.False);
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }
    }
}
