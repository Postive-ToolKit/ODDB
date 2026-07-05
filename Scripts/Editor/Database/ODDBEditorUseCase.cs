using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Editors.Commands;
using TeamODD.ODDB.Editors.Settings;
using TeamODD.ODDB.Editors.UI.Progress;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Utils.Sheets;
using TeamODD.ODDB.Editors.Utils.Sheets.Diff;
using TeamODD.ODDB.Editors.Utils.Sheets.Validation;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace TeamODD.ODDB.Editors.Window
{
    /// <summary>
    /// The core use case class for the ODDB Editor. 
    /// Handles data access, manipulation, and command history (Undo/Redo).
    /// Acts as a bridge between the UI and the Data Logic.
    /// </summary>
    public class ODDBEditorUseCase : IODDBEditorUseCase
    {
        public IODDatabase DataBase => _database;

        /// <summary>
        /// Triggered when a specific view data is changed or removed.
        /// </summary>
        public event Action<string> OnViewChanged;

        /// <summary>
        /// Triggered when the undo/redo history changes.
        /// </summary>
        public event Action OnHistoryChanged;

        private ODDatabase _database;
        private readonly CommandProcessor _commandProcessor = new();
        private string _selectedTableId;
        private const int PreImportBackupKeep = 3;
        private const int PreSaveBackupKeep = 3;
        private ODDBLoadReport _loadReport;

        /// <summary>
        /// True iff Save is allowed. False when the most recent load surfaced a fatal
        /// failure via <see cref="ODDatabase.TryLoad"/>. Bootstrap (fresh install) leaves
        /// <c>_loadReport == null</c> and returns true.
        /// </summary>
        public bool CanSave => _loadReport == null || _loadReport.IsSafeToSave;

        /// <summary>Diagnostics: the load report from the most recent ctor load attempt, or null on fresh-install bootstrap.</summary>
        public ODDBLoadReport LoadReport => _loadReport;

        public ODDBEditorUseCase()
        {
            var editorSettings = ODDBEditorSettings.TryLoad();
            _commandProcessor.MaxHistoryCount = editorSettings?.MaxHistoryCount ?? 50;
            _commandProcessor.OnHistoryChanged += HandleHistoryChanged;

            var runtimeSettings = ODDBRuntimeSettings.TryLoad();
            var verbose = runtimeSettings != null && runtimeSettings.UseDebugLog;

            string fullPath = ODDBRuntimeSettings.ResolveDatabasePath(runtimeSettings);

            var fileExisted = File.Exists(fullPath);
            long fileSize = fileExisted ? new FileInfo(fullPath).Length : 0;
            if (verbose)
                Debug.Log($"[ODDB] Load path={fullPath} exists={fileExisted} size={fileSize}B");

            if (fileExisted)
            {
                if (ODDatabase.TryLoad(fullPath, out _database, out _loadReport))
                {
                    if (verbose) DumpLoadDiagnostics(fullPath);
                }
                else
                {
                    // TryLoad may have exposed a partial DB for read-only inspection
                    // (EmptyRestoredOnNonEmptyDto / UnmappedFieldType); fall back to
                    // CreateEmpty when nothing was exposed.
                    _database ??= ODDatabase.CreateEmpty();
                    // Always-on diagnostic log; not gated on UseDebugLog.
                    Debug.LogError(
                        $"[ODDB][LOAD-FATAL] stage={_loadReport.FailureStage} reason={_loadReport.FailureReason} " +
                        $"path={fullPath} size={_loadReport.FileSize} " +
                        $"dto-tables={_loadReport.DtoTableCount} dto-views={_loadReport.DtoViewCount} " +
                        $"restored-tables={_loadReport.RestoredTableCount} restored-views={_loadReport.RestoredViewCount} " +
                        $"unmapped={_loadReport.UnmappedFieldTypeCount} fmt={_loadReport.SourceFormatVersion}");
                }
            }
            else
            {
                // Fresh install — explicit intent, no load attempt; CanSave returns true.
                _database = ODDatabase.CreateEmpty();
                _loadReport = null;
                Debug.LogWarning($"[ODDB] Database not found at {fullPath} — created an empty one. If you have an existing ODDB.bytes elsewhere, set its location in Assets/Resources/ODDBRuntimeSettings.asset (Path field).");
                try
                {
                    _database.Save(fullPath);
                    AssetDatabase.Refresh();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Initial DB save failed: {ex.Message}");
                }
            }

            _database.OnDataChanged += OnDataChanged;
            _database.OnDataRemoved += OnDataChanged;
        }

        /// <summary>
        /// Dump per-table/view summary to the console after load. Useful for diagnosing
        /// migration / corruption issues where the editor opens a non-empty file as empty.
        /// </summary>
        public void DumpLoadDiagnostics(string fullPath)
        {
            if (_database == null)
            {
                Debug.LogError($"[ODDB] Diagnostics: _database is null after Load({fullPath})");
                return;
            }
            var views = _database.Views?.GetAll();
            var tables = _database.Tables?.GetAll();
            int viewCount = views == null ? 0 : System.Linq.Enumerable.Count(views);
            int tableCount = tables == null ? 0 : System.Linq.Enumerable.Count(tables);
            Debug.Log($"[ODDB] After load: views={viewCount} tables={tableCount} (path={fullPath})");

            if (views != null)
            {
                int i = 0;
                foreach (var v in views)
                    Debug.Log($"[ODDB]   view[{i++}] id={v?.ID} name={v?.Name} fields={v?.TotalFields?.Count ?? 0} bindType={v?.BindType?.FullName ?? "<null>"}");
            }
            if (tables != null)
            {
                int i = 0;
                foreach (var v in tables)
                {
                    var t = v as Table;
                    Debug.Log($"[ODDB]   table[{i++}] id={t?.ID} name={t?.Name} fields={t?.TotalFields?.Count ?? 0} rows={t?.Rows?.Count ?? 0} bindType={t?.BindType?.FullName ?? "<null>"}");
                }
            }
        }

        private void OnDataChanged(ODDBID id)
        {
            OnViewChanged?.Invoke(id.ToString());
        }

        private void HandleHistoryChanged()
        {
            OnHistoryChanged?.Invoke();
        }

        public IView GetViewByKey(string id)
        {
            if (_database == null)
                return null;
            var view = _database.GetView(new ODDBID(id));
            return view;
        }

        public IEnumerable<IView> GetViews(Predicate<IView> predicate = null)
        {
            if (_database == null)
                return null;
            if (predicate == null)
                return _database.GetAll();
            var query = _database.GetAll().Where(x => predicate(x));
            return query;
        }

        public ODDBViewType GetViewTypeByKey(string id)
        {
            var view = GetViewByKey(id);
            if (view is Table)
                return ODDBViewType.Table;
            if (view is View)
                return ODDBViewType.View;
            return ODDBViewType.None;
        }

        public string GetViewName(string id)
        {
            return GetViewByKey(id)?.Name ?? string.Empty;
        }

        /// <summary>
        /// Executes a command to change the name of a View or Table.
        /// </summary>
        public void SetViewName(string id, string name)
        {
            var view = GetViewByKey(id);
            if (view == null) return;
            
            var command = new SetViewNameCommand(view, name, (viewId) => _database.NotifyDataChanged(new ODDBID(viewId)));
            _commandProcessor.Execute(command);
        }

        public void SetViewId(string id, string newId)
        {
            id = NormalizeId(id);
            newId = NormalizeId(newId);
            ValidateNewId(newId, "new view id");

            var view = GetViewByKey(id);
            if (view == null)
                throw new InvalidOperationException($"View or Table '{id}' was not found.");
            if (string.Equals(id, newId, StringComparison.Ordinal))
                return;
            if (GetViewByKey(newId) != null)
                throw new InvalidOperationException($"View or Table ID '{newId}' already exists.");

            var repository = view is Table ? _database.Tables : _database.Views;
            var command = new SetViewIdCommand(
                repository,
                new ODDBID(id),
                new ODDBID(newId),
                viewId => _database.NotifyDataChanged(new ODDBID(viewId)));
            _commandProcessor.Execute(command);

            if (string.Equals(_selectedTableId, id, StringComparison.Ordinal))
                _selectedTableId = newId;
        }

        /// <summary>
        /// Executes a command to create a new Table via the undo/redo pipeline.
        /// </summary>
        public void AddTable()
        {
            if (_database == null) return;
            var command = new AddViewItemCommand(
                _database.Tables,
                "Add Table",
                id => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to create a new View via the undo/redo pipeline.
        /// </summary>
        public void AddView()
        {
            if (_database == null) return;
            var command = new AddViewItemCommand(
                _database.Views,
                "Add View",
                id => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to delete a Table by id via the undo/redo pipeline.
        /// </summary>
        public void RemoveTable(string tableId)
        {
            if (_database == null || string.IsNullOrEmpty(tableId)) return;
            var command = new RemoveViewItemCommand(
                _database.Tables,
                new ODDBID(tableId),
                "Remove Table",
                id => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to delete a View by id via the undo/redo pipeline.
        /// </summary>
        public void RemoveView(string viewId)
        {
            if (_database == null || string.IsNullOrEmpty(viewId)) return;
            var command = new RemoveViewItemCommand(
                _database.Views,
                new ODDBID(viewId),
                "Remove View",
                id => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to add a new row to a Table.
        /// </summary>
        public void AddRow(string tableId)
        {
            var view = GetViewByKey(tableId);
            if (view is not Table table) return;

            var command = new AddRowCommand(table, (id) => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to remove a row from a Table.
        /// </summary>
        public void RemoveRow(string tableId, string rowId)
        {
            var view = GetViewByKey(tableId);
            if (view is not Table table) return;

            var command = new RemoveRowCommand(table, rowId, (id) => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        public void SetRowId(string tableId, string rowId, string newRowId)
        {
            tableId = NormalizeId(tableId);
            rowId = NormalizeId(rowId);
            newRowId = NormalizeId(newRowId);
            ValidateNewId(newRowId, "new row id");

            var view = GetViewByKey(tableId);
            if (view is not Table table)
                throw new InvalidOperationException($"Table '{tableId}' was not found.");
            if (table.GetRow(rowId) == null)
                throw new InvalidOperationException($"Row '{rowId}' was not found in table '{tableId}'.");
            if (string.Equals(rowId, newRowId, StringComparison.Ordinal))
                return;
            var duplicate = GetRow(newRowId);
            if (duplicate != null)
                throw new InvalidOperationException($"Row ID '{newRowId}' already exists.");

            var command = new SetRowIdCommand(table, rowId, newRowId, id => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Routes a single-cell mutation through the command pipeline (undo/redo, history).
        /// UI passes a pre-serialized string with direct=true; programmatic callers
        /// (e.g. MCP) pass a typed value with direct=false.
        /// </summary>
        public void SetCellData(string tableId, string rowId, int fieldIndex, object newValue, bool direct = false)
        {
            var view = GetViewByKey(tableId);
            if (view is not Table table) return;

            var command = new SetCellDataCommand(
                table, rowId, fieldIndex, newValue,
                (id) => _database.NotifyDataChanged(new ODDBID(id)),
                direct);
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to add a new field to a View.
        /// </summary>
        public void AddField(string viewId, Field field)
        {
            var view = GetViewByKey(viewId);
            if (view == null) return;

            var command = new AddFieldCommand(view, field, (id) => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to remove a field from a View.
        /// </summary>
        public void RemoveField(string viewId, int index)
        {
            var view = GetViewByKey(viewId);
            if (view == null) return;

            var command = new RemoveFieldCommand(view, index, (id) => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        public void MoveField(string viewId, int oldIndex, int newIndex)
        {
            var view = GetViewByKey(viewId);
            if (view == null) return;

            var command = new MoveFieldCommand(view, oldIndex, newIndex, (id) => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        public void SetFieldType(string viewId, int fieldIndex, string typeKey, string param)
        {
            var view = GetViewByKey(viewId);
            if (view == null) return;

            var command = new SetFieldTypeCommand(view, fieldIndex, typeKey, param, (id) => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        public void MoveViewItem(string viewId, int oldSiblingIndex, int newSiblingIndex)
        {
            if (!ViewSiblingOrderResolver.TryResolveMove(
                    _database,
                    viewId,
                    oldSiblingIndex,
                    newSiblingIndex,
                    out var repository,
                    out var oldRepositoryIndex,
                    out var newRepositoryIndex))
                return;

            var command = new MoveViewItemCommand(
                repository,
                oldRepositoryIndex,
                newRepositoryIndex,
                id => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        public Type GetViewBindType(string id)
        {
            var view = GetViewByKey(id);
            if (view == null)
                return null;
            return view.BindType;
        }

        /// <summary>
        /// Executes a command to change the bind type of a View.
        /// </summary>
        public void SetViewBindType(string id, Type type)
        {
            var view = GetViewByKey(id);
            if (view == null) return;

            var command = new SetBindTypeCommand(view, type, (viewId) => _database.NotifyDataChanged(new ODDBID(viewId)));
            _commandProcessor.Execute(command);
        }

        public IView GetViewParent(string id)
        {
            var view = GetViewByKey(id);
            if (view == null)
                return null;
            return view.ParentView;
        }

        /// <summary>
        /// Executes a command to change the parent view of a View.
        /// </summary>
        public void SetViewParent(string id, string parentKey)
        {
            var view = GetViewByKey(id);
            if (view == null) return;
            var parent = GetViewByKey(parentKey);
            
            if (parent == null && !string.IsNullOrEmpty(parentKey)) return; 

            var command = new SetParentCommand(view, parent, (viewId) => _database.NotifyDataChanged(new ODDBID(viewId)));
            _commandProcessor.Execute(command);
        }

        public void NotifyViewDataChanged(string viewId)
        {
            _database.NotifyDataChanged(new ODDBID(viewId));
        }

        public IEnumerable<IView> GetPureViews()
        {
            return _database.Views.GetAll();
        }

        public IEnumerable<Row> GetViewRows(string viewId)
        {
            var tableList = GetInheritedTables(viewId).ToList();
            var rows = new List<Row>();
            foreach (var table in tableList)
                rows.AddRange(table.Rows);
            return rows;
        }
        
        public IEnumerable<Table> GetInheritedTables(string viewId)
        {
            var view = GetViewByKey(viewId);
            if (view == null)
                return Enumerable.Empty<Table>();

            var children = _database.GetAll()
                .Where(v => v.ParentView != null && v.ParentView.ID == view.ID);
            var tables = new List<Table>();
            
            if (view is Table tableView)
                tables.Add(tableView);
            
            foreach (var child in children)
            {
                if (child is Table table)
                    tables.Add(table);
                tables.AddRange(GetInheritedTables(child.ID.ToString()));
            }
            return tables;
        }

        public Row GetRow(string rowId)
        {
            foreach (var view in _database.Tables.GetAll())
            {
                var table = view as Table;
                var row = table.GetRow(rowId);
                if (row != null)
                    return row;
            }
            return null;
        }

        public bool TryGetRow(string viewId, string rowId, out Row row)
        {
            row = null;
            var getViewRows = GetViewRows(viewId).ToList();
            if (getViewRows.Count == 0)
                return false;
            row = getViewRows.FirstOrDefault(r => r.ID.ToString() == rowId);
            return row != null;
        }

        public void SaveDatabase(string fullPath)
        {
            if (!CanSave)
            {
                Debug.LogError(
                    $"[ODDB][SAVE-REFUSED] reason={_loadReport?.FailureStage ?? "unknown"} " +
                    $"path={fullPath} callsite=SaveDatabase");
                return;
            }
            ODDBBackup.CreatePreSaveBackup(fullPath, PreSaveBackupKeep);
            _database.Save(fullPath);
            _commandProcessor.MarkSaved();
        }

        public bool IsDirty => _commandProcessor.IsDirty;
        public void MarkSaved() => _commandProcessor.MarkSaved();

        public void Undo() => _commandProcessor.Undo();
        public void Redo() => _commandProcessor.Redo();
        
        public IEnumerable<ICommand> GetUndoHistory() => _commandProcessor.GetUndoList();
        public IEnumerable<ICommand> GetRedoHistory() => _commandProcessor.GetRedoList();
        public void JumpToHistory(ICommand command) => _commandProcessor.JumpTo(command);

        public void SetSelectionContext(string tableId)
        {
            _selectedTableId = string.IsNullOrEmpty(tableId) ? null : tableId;
        }

        public bool TryGetSelectedTableId(out string tableId)
        {
            tableId = _selectedTableId;
            return !string.IsNullOrEmpty(tableId);
        }

        public async Task ExportAsync(
            ExportScope scope,
            ISheetBackend backend,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            if (_database == null) throw new InvalidOperationException("Database not loaded.");

            var ctx = await backend.PrepareAsync(scope, BackendIntent.Export);
            if (ctx.Cancelled) return;

            var stopwatch = Stopwatch.StartNew();
            var sheetCount = 0;
            var rowCount = 0;
            var status = "success";
            try
            {
                EditorApplication.LockReloadAssemblies();
                var sheets = CollectSheets(scope);
                sheetCount = sheets.Count;
                rowCount = TotalDataRows(sheets);
                await backend.SaveAsync(ctx, sheets, progress, ct);
            }
            catch (OperationCanceledException)
            {
                status = "cancelled";
                throw;
            }
            catch (Exception)
            {
                status = "failure";
                throw;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                stopwatch.Stop();
                LogOperation("Export", backend, scope, sheetCount, rowCount, stopwatch.ElapsedMilliseconds, status, null);
            }
        }

        public async Task ImportAsync(
            ExportScope scope,
            ISheetBackend backend,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            if (_database == null) throw new InvalidOperationException("Database not loaded.");

            var ctx = await backend.PrepareAsync(scope, BackendIntent.Import);
            if (ctx.Cancelled) return;

            var backupPath = (string)null;
            var stopwatch = Stopwatch.StartNew();
            var sheetCount = 0;
            var rowCount = 0;
            var status = "success";
            try
            {
                EditorApplication.LockReloadAssemblies();
                var sheets = await backend.LoadAsync(ctx, progress, ct);
                var validationReport = SheetImportValidator.Validate(sheets, scope, _database);
                if (validationReport.Issues.Count > 0)
                {
                    var summary = validationReport.ToSummaryString();
                    if (validationReport.HasErrors)
                        throw new InvalidOperationException(summary);
                    Debug.LogWarning(summary);
                }

                var diffReport = SheetImportDiffBuilder.Build(sheets, scope, _database);
                var accepted = progress is IODDBImportPreviewPresenter presenter
                    ? await presenter.ShowImportPreviewAsync(diffReport, validationReport, ct)
                    : true;

                if (!accepted)
                    throw new OperationCanceledException("Import cancelled from preview window.");

                backupPath = CreatePreImportBackup();
                var converter = new ODDBSheetConverter();
                var affected = ApplySheetsToDatabase(scope, sheets, converter);
                sheetCount = affected.Count;
                rowCount = TotalDataRows(affected);
                PersistDatabase();
                _commandProcessor.Clear();
                NotifyAffectedViews(affected);
            }
            catch (OperationCanceledException)
            {
                status = "cancelled";
                throw;
            }
            catch (Exception)
            {
                status = "failure";
                throw;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                stopwatch.Stop();
                LogOperation("Import", backend, scope, sheetCount, rowCount, stopwatch.ElapsedMilliseconds, status, backupPath);
            }
        }

        private IReadOnlyList<SheetInfo> CollectSheets(ExportScope scope)
        {
            var converter = new ODDBSheetConverter();
            if (scope.All) return converter.GetAllSheets();

            if (_database.Tables.Read(new ODDBID(scope.TargetTableId)) is not Table table)
                throw new InvalidOperationException(
                    $"Table '{scope.TargetTableId}' not found in current database.");
            return new List<SheetInfo> { converter.ExportTable(table) };
        }

        private IReadOnlyList<SheetInfo> ApplySheetsToDatabase(
            ExportScope scope, IReadOnlyList<SheetInfo> sheets, ODDBSheetConverter converter)
        {
            var applied = new List<SheetInfo>();
            if (scope.All)
            {
                foreach (var sheet in sheets)
                {
                    if (sheet == null) continue;
                    if (sheet.Name != null && sheet.Name.StartsWith(SheetConfig.IGNORE_PREFIX)) continue;
                    if (_database.Tables.Read(new ODDBID(sheet.ID)) is not Table table)
                    {
                        Debug.LogWarning($"Import: table {sheet.ID} not found in current database; skipping.");
                        continue;
                    }
                    converter.ApplySheetToTable(table, sheet);
                    applied.Add(sheet);
                }
                return applied;
            }

            var targetSheet = sheets.FirstOrDefault(s => s != null && s.ID == scope.TargetTableId);
            if (targetSheet == null)
                throw new InvalidOperationException(
                    $"No sheet found for table id '{scope.TargetTableId}'.");
            if (_database.Tables.Read(new ODDBID(scope.TargetTableId)) is not Table targetTable)
                throw new InvalidOperationException(
                    $"Table '{scope.TargetTableId}' not found in current database.");
            converter.ApplySheetToTable(targetTable, targetSheet);
            applied.Add(targetSheet);
            return applied;
        }

        private void PersistDatabase()
        {
            var fullPath = ODDBRuntimeSettings.ResolveDatabasePath();
            if (!CanSave)
            {
                Debug.LogError(
                    $"[ODDB][SAVE-REFUSED-PERSIST] reason={_loadReport?.FailureStage ?? "unknown"} " +
                    $"path={fullPath} callsite=PersistDatabase");
                return;
            }
            ODDBBackup.CreatePreSaveBackup(fullPath, PreSaveBackupKeep);
            _database.Save(fullPath);
            _commandProcessor.MarkSaved();
        }

        private void NotifyAffectedViews(IReadOnlyList<SheetInfo> sheets)
        {
            foreach (var sheet in sheets)
            {
                if (sheet == null || string.IsNullOrEmpty(sheet.ID)) continue;
                _database.NotifyDataChanged(new ODDBID(sheet.ID));
            }
        }

        private string CreatePreImportBackup()
        {
            if (_database == null || ODDBRuntimeSettings.Setting == null) return null;
            var fullPath = ODDBRuntimeSettings.ResolveDatabasePath();
            if (!File.Exists(fullPath)) return null;

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var backupPath = $"{fullPath}.preimport-{timestamp}.bak";
            try
            {
                File.Copy(fullPath, backupPath, true);
                ODDBBackup.RotateBackups(fullPath, PreImportBackupKeep, ".preimport-*.bak");
                return backupPath;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Pre-import backup failed: {e.Message}");
                return null;
            }
        }

        private static int TotalDataRows(IReadOnlyList<SheetInfo> sheets)
        {
            var total = 0;
            foreach (var sheet in sheets)
            {
                var count = sheet?.Values?.Count ?? 0;
                if (count > 2) total += count - 2;
            }
            return total;
        }

        private static void LogOperation(
            string intent,
            ISheetBackend backend,
            ExportScope scope,
            int sheetCount,
            int rowCount,
            long durationMs,
            string status,
            string backupPath)
        {
            var backendName = (backend.DisplayName ?? "unknown").ToLowerInvariant().Replace(' ', '_');
            var message =
                $"[ODDB:{intent}] backend={backendName} scope={scope} sheets={sheetCount} rows={rowCount} duration={durationMs}ms status={status}";
            if (!string.IsNullOrEmpty(backupPath))
                message += $" backupPath={backupPath}";
            Debug.Log(message);
        }

        private static string NormalizeId(string id)
        {
            return id?.Trim() ?? string.Empty;
        }

        private static void ValidateNewId(string id, string label)
        {
            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException($"{label} cannot be empty.");
        }

        public void Dispose()
        {
            _commandProcessor.OnHistoryChanged -= HandleHistoryChanged;

            _database.OnDataChanged -= OnDataChanged;
            _database.OnDataRemoved -= OnDataChanged;
            _database = null;
            OnViewChanged = null;
            OnHistoryChanged = null;
            _commandProcessor.Clear();
        }
    }
}
