using System;
using System.IO;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class GoogleSheetsBindingStoreTests
    {
        private string _directory;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ODDB-GoogleSheetsBindingTests-" + Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, "bindings.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void Upsert_PersistsBindingAndSpreadsheetIsolation()
        {
            var store = GoogleSheetsBindingStore.Load(_path);
            store.Upsert("spreadsheet-a", "item", 0, "Items");
            store.Upsert("spreadsheet-a", "enemy", 10, "Enemies");
            store.Upsert("spreadsheet-b", "item", 20, "Other Items");

            var reloaded = GoogleSheetsBindingStore.Load(_path);

            Assert.That(reloaded.TryGet("spreadsheet-a", "item", out var first), Is.True);
            Assert.That(first.sheetId, Is.Zero);
            Assert.That(first.lastKnownTitle, Is.EqualTo("Items"));
            Assert.That(reloaded.TryGet("spreadsheet-a", "enemy", out var sameSpreadsheet), Is.True);
            Assert.That(sameSpreadsheet.sheetId, Is.EqualTo(10));
            Assert.That(reloaded.TryGet("spreadsheet-b", "item", out var second), Is.True);
            Assert.That(second.sheetId, Is.EqualTo(20));
        }

        [Test]
        public void Upsert_ReplacesMissingRemoteSheetBindingForSameTable()
        {
            var store = GoogleSheetsBindingStore.Load(_path);
            store.Upsert("spreadsheet", "item", 10, "Old");
            store.Upsert("spreadsheet", "item", 11, "New");

            var reloaded = GoogleSheetsBindingStore.Load(_path);

            Assert.That(reloaded.TryGet("spreadsheet", "item", out var binding), Is.True);
            Assert.That(binding.sheetId, Is.EqualTo(11));
            Assert.That(binding.lastKnownTitle, Is.EqualTo("New"));
        }

        [Test]
        public void Upsert_RejectsOneSheetBoundToDifferentTables()
        {
            var store = GoogleSheetsBindingStore.Load(_path);
            store.Upsert("spreadsheet", "item", 10, "Items");

            Assert.That(
                () => store.Upsert("spreadsheet", "enemy", 10, "Enemies"),
                Throws.InvalidOperationException);
        }

        [Test]
        public void Load_InvalidJsonStartsFreshAndCanRecover()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_path, "{ invalid json");

            var store = GoogleSheetsBindingStore.Load(_path);
            store.Upsert("spreadsheet", "item", 10, "Items");

            var reloaded = GoogleSheetsBindingStore.Load(_path);
            Assert.That(reloaded.TryGet("spreadsheet", "item", out var binding), Is.True);
            Assert.That(binding.sheetId, Is.EqualTo(10));
        }
    }
}
