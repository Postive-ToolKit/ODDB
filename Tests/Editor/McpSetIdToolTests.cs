using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Commands;
using TeamODD.ODDB.Editors.MCP.Tools.Data;
using TeamODD.ODDB.Editors.MCP.Tools.Schema;
using TeamODD.ODDB.Editors.Utils.Sheets;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class McpSetIdToolTests
    {
        [Test]
        public void SetViewIdTool_ExecutesUseCaseAndReturnsNewId()
        {
            var useCase = new FakeUseCase();
            var tool = new SetViewIdTool(useCase);

            var result = JObject.FromObject(tool.Execute(new JObject
            {
                ["viewId"] = "view",
                ["newViewId"] = "view-renamed",
            }));

            Assert.That(useCase.SetViewIdCalls, Is.EqualTo(new[] { ("view", "view-renamed") }));
            Assert.That(result["success"]?.ToObject<bool>(), Is.True);
            Assert.That(result["oldViewId"]?.ToString(), Is.EqualTo("view"));
            Assert.That(result["viewId"]?.ToString(), Is.EqualTo("view-renamed"));
            Assert.That(result["affectedViewId"]?.ToString(), Is.EqualTo("view-renamed"));
        }

        [Test]
        public void SetRowIdTool_ExecutesUseCaseAndReturnsNewId()
        {
            var useCase = new FakeUseCase();
            var tool = new SetRowIdTool(useCase);

            var result = JObject.FromObject(tool.Execute(new JObject
            {
                ["tableId"] = "table",
                ["rowId"] = "row",
                ["newRowId"] = "row-renamed",
            }));

            Assert.That(useCase.SetRowIdCalls, Is.EqualTo(new[] { ("table", "row", "row-renamed") }));
            Assert.That(result["success"]?.ToObject<bool>(), Is.True);
            Assert.That(result["tableId"]?.ToString(), Is.EqualTo("table"));
            Assert.That(result["oldRowId"]?.ToString(), Is.EqualTo("row"));
            Assert.That(result["rowId"]?.ToString(), Is.EqualTo("row-renamed"));
            Assert.That(result["affectedViewId"]?.ToString(), Is.EqualTo("table"));
        }

        private sealed class FakeUseCase : IODDBEditorUseCase
        {
            private readonly ODDatabase _database = new ODDatabase();

            public readonly List<(string CurrentId, string NewId)> SetViewIdCalls = new();
            public readonly List<(string TableId, string CurrentId, string NewId)> SetRowIdCalls = new();

            public FakeUseCase()
            {
                _database.Views.Create(new ODDBID("view"));
                var table = (Table)_database.Tables.Create(new ODDBID("table"));
                table.AddRow(new ODDBID("row"));
            }

            public event Action<string> OnViewChanged;
            public event Action OnHistoryChanged
            {
                add { }
                remove { }
            }

            public IODDatabase DataBase => _database;

            public IView GetViewByKey(string key) => _database.GetView(new ODDBID(key));

            public IEnumerable<IView> GetViews(Predicate<IView> predicate = null)
            {
                var views = _database.GetAll();
                return predicate == null ? views : views.Where(view => predicate(view));
            }

            public ODDBViewType GetViewTypeByKey(string key)
            {
                return GetViewByKey(key) is Table ? ODDBViewType.Table : ODDBViewType.View;
            }

            public string GetViewName(string key) => GetViewByKey(key)?.Name ?? string.Empty;
            public void SetViewName(string key, string name) => throw new NotImplementedException();
            public void SetViewId(string key, string newKey) => SetViewIdCalls.Add((key, newKey));
            public void AddTable() => throw new NotImplementedException();
            public void AddView() => throw new NotImplementedException();
            public void RemoveTable(string tableId) => throw new NotImplementedException();
            public void RemoveView(string viewId) => throw new NotImplementedException();
            public void AddRow(string tableId) => throw new NotImplementedException();
            public void RemoveRow(string tableId, string rowId) => throw new NotImplementedException();
            public void SetRowId(string tableId, string rowId, string newRowId) => SetRowIdCalls.Add((tableId, rowId, newRowId));
            public void AddField(string viewId, Field field) => throw new NotImplementedException();
            public void RemoveField(string viewId, int index) => throw new NotImplementedException();
            public void MoveField(string viewId, int oldIndex, int newIndex) => throw new NotImplementedException();
            public void SetFieldType(string viewId, int fieldIndex, string typeKey, string param) => throw new NotImplementedException();
            public void MoveViewItem(string viewId, int oldSiblingIndex, int newSiblingIndex) => throw new NotImplementedException();
            public void SetCellData(string tableId, string rowId, int fieldIndex, object newValue, bool direct = false) => throw new NotImplementedException();
            public Type GetViewBindType(string key) => throw new NotImplementedException();
            public void SetViewBindType(string key, Type type) => throw new NotImplementedException();
            public IView GetViewParent(string key) => throw new NotImplementedException();
            public void SetViewParent(string key, string parentKey) => throw new NotImplementedException();
            public void NotifyViewDataChanged(string viewId) => OnViewChanged?.Invoke(viewId);
            public IEnumerable<IView> GetPureViews() => _database.Views.GetAll();
            public IEnumerable<Row> GetViewRows(string viewId) => ((Table)GetViewByKey(viewId)).Rows;
            public IEnumerable<Table> GetInheritedTables(string viewId) => throw new NotImplementedException();
            public Row GetRow(string rowId)
            {
                foreach (var view in _database.Tables.GetAll())
                {
                    if (view is Table table && table.GetRow(rowId) is { } row)
                        return row;
                }
                return null;
            }
            public bool TryGetRow(string viewId, string rowId, out Row row)
            {
                row = ((Table)GetViewByKey(viewId)).GetRow(rowId);
                return row != null;
            }
            public void SaveDatabase(string fullPath) => throw new NotImplementedException();
            public bool IsDirty => false;
            public void MarkSaved() => throw new NotImplementedException();
            public void Undo() => throw new NotImplementedException();
            public void Redo() => throw new NotImplementedException();
            public IEnumerable<ICommand> GetUndoHistory() => throw new NotImplementedException();
            public IEnumerable<ICommand> GetRedoHistory() => throw new NotImplementedException();
            public void JumpToHistory(ICommand command) => throw new NotImplementedException();
            public void SetSelectionContext(string tableId) => throw new NotImplementedException();
            public bool TryGetSelectedTableId(out string tableId)
            {
                tableId = null;
                return false;
            }
            public Task ExportAsync(ExportScope scope, ISheetBackend backend, IProgress<float> progress = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task ImportAsync(ExportScope scope, ISheetBackend backend, IProgress<float> progress = null, CancellationToken ct = default) => throw new NotImplementedException();
            public void Dispose() { }
        }
    }
}
