using System;
using System.Collections.Generic;
using System.Globalization;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Settings;

namespace TeamODD.ODDB.Editors
{
    /// <summary>
    /// Public integration point for custom Unity editor tools that edit ODDB data.
    /// The built-in ODDB window, MCP server, and external editor windows all share
    /// this process-lifetime session and therefore share data, undo history, and save state.
    /// </summary>
    public sealed class ODDBEditorSession
    {
        private readonly IODDBEditorUseCase _useCase;

        internal ODDBEditorSession(IODDBEditorUseCase useCase)
        {
            _useCase = useCase ?? throw new ArgumentNullException(nameof(useCase));
        }

        /// <summary>Gets the editor-process ODDB session.</summary>
        public static ODDBEditorSession Current => ODDBEditorRuntime.Session;

        /// <summary>
        /// ODDB's editor use cases. Route mutations through this object so they remain
        /// undoable and immediately refresh every ODDB editor using the shared session.
        /// </summary>
        public IODDBEditorUseCase Commands => _useCase;

        /// <summary>The current in-memory database. This reference changes after a reload.</summary>
        public IODDatabase Database => _useCase.DataBase;

        /// <summary>The absolute path used by <see cref="Save"/>.</summary>
        public string DatabasePath => ODDBRuntimeSettings.ResolveDatabasePath();

        public bool IsDirty => _useCase.IsDirty;

        /// <summary>False when ODDB detected a fatal load problem and protects the source file.</summary>
        public bool CanSave => _useCase is not ODDBEditorUseCase concrete || concrete.CanSave;

        /// <summary>Gets an exact table by ID. Views and missing IDs are rejected.</summary>
        public Table GetTable(string tableId)
        {
            if (TryGetTable(tableId, out var table))
                return table;

            throw new KeyNotFoundException($"ODDB table '{tableId}' was not found.");
        }

        public bool TryGetTable(string tableId, out Table table)
        {
            table = null;
            if (string.IsNullOrWhiteSpace(tableId))
                return false;

            table = _useCase.GetViewByKey(tableId) as Table;
            return table != null;
        }

        /// <summary>
        /// Returns a snapshot of every row belonging directly to the specified table.
        /// Child-table rows are not included.
        /// </summary>
        public IReadOnlyList<Row> GetRows(string tableId)
        {
            return new List<Row>(GetTable(tableId).Rows);
        }

        /// <summary>Gets the TotalFields index for a field name, including inherited fields.</summary>
        public int GetFieldIndex(string tableId, string fieldName)
        {
            var table = GetTable(tableId);
            if (TryGetFieldIndex(table, fieldName, out var fieldIndex))
                return fieldIndex;

            throw new KeyNotFoundException(
                $"ODDB field '{fieldName}' was not found in table '{tableId}'.");
        }

        public bool TryGetFieldIndex(string tableId, string fieldName, out int fieldIndex)
        {
            fieldIndex = -1;
            return TryGetTable(tableId, out var table)
                   && TryGetFieldIndex(table, fieldName, out fieldIndex);
        }

        /// <summary>
        /// Gets a live cell by table, row, and field name. Treat the returned cell as
        /// read-only; use <see cref="Commands"/> to make undoable changes.
        /// </summary>
        public Cell GetCell(string tableId, string rowId, string fieldName)
        {
            if (TryGetCell(tableId, rowId, fieldName, out var cell))
                return cell;

            throw new KeyNotFoundException(
                $"ODDB cell '{tableId}/{rowId}/{fieldName}' was not found.");
        }

        public bool TryGetCell(string tableId, string rowId, string fieldName, out Cell cell)
        {
            cell = null;
            if (!TryGetTable(tableId, out var table)
                || string.IsNullOrWhiteSpace(rowId)
                || table.GetRow(rowId) is not Row row
                || !TryGetFieldIndex(table, fieldName, out var fieldIndex)
                || fieldIndex < 0
                || fieldIndex >= row.Cells.Count)
            {
                return false;
            }

            cell = row.GetData(fieldIndex);
            return cell != null;
        }

        /// <summary>Gets and converts a deserialized cell value.</summary>
        public T GetValue<T>(string tableId, string rowId, string fieldName)
        {
            if (TryGetValue(tableId, rowId, fieldName, out T value))
                return value;

            throw new InvalidOperationException(
                $"ODDB value '{tableId}/{rowId}/{fieldName}' was missing or could not be converted to {typeof(T).Name}.");
        }

        public bool TryGetValue<T>(string tableId, string rowId, string fieldName, out T value)
        {
            value = default;
            if (!TryGetCell(tableId, rowId, fieldName, out var cell))
                return false;

            object data;
            try
            {
                data = cell.GetData();
            }
            catch
            {
                return false;
            }

            if (data is T typed)
            {
                value = typed;
                return true;
            }

            var targetType = typeof(T);
            var nullableType = Nullable.GetUnderlyingType(targetType);
            if (data == null)
                return !targetType.IsValueType || nullableType != null;

            var conversionType = nullableType ?? targetType;
            try
            {
                object converted;
                if (conversionType.IsEnum)
                {
                    converted = data is string enumName
                        ? Enum.Parse(conversionType, enumName, false)
                        : Enum.ToObject(conversionType, data);
                }
                else
                {
                    converted = Convert.ChangeType(data, conversionType, CultureInfo.InvariantCulture);
                }

                value = (T)converted;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Raised after a view/table schema or row value changes. Null means a full reload.</summary>
        public event Action<string> ViewChanged
        {
            add => _useCase.OnViewChanged += value;
            remove => _useCase.OnViewChanged -= value;
        }

        /// <summary>Raised when undo/redo history or the dirty/saved state changes.</summary>
        public event Action StateChanged
        {
            add => _useCase.OnHistoryChanged += value;
            remove => _useCase.OnHistoryChanged -= value;
        }

        /// <summary>Saves the shared in-memory database to the configured ODDB path.</summary>
        public void Save()
        {
            if (!CanSave)
                throw new InvalidOperationException(
                    "ODDB save is disabled because the current database did not load safely.");

            _useCase.SaveDatabase(DatabasePath);
        }

        /// <summary>Attempts to save without requiring a custom editor to implement exception handling.</summary>
        public bool TrySave(out string error)
        {
            try
            {
                Save();
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public void Undo() => _useCase.Undo();
        public void Redo() => _useCase.Redo();

        private static bool TryGetFieldIndex(Table table, string fieldName, out int fieldIndex)
        {
            fieldIndex = -1;
            if (table == null || string.IsNullOrWhiteSpace(fieldName))
                return false;

            for (var i = 0; i < table.TotalFields.Count; i++)
            {
                if (!string.Equals(table.TotalFields[i]?.Name, fieldName, StringComparison.Ordinal))
                    continue;

                fieldIndex = i;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reloads the configured database while preserving this session and all event subscriptions.
        /// Unsaved edits must be explicitly discarded.
        /// </summary>
        public void ReloadFromDisk(bool discardUnsavedChanges = false)
        {
            if (IsDirty && !discardUnsavedChanges)
                throw new InvalidOperationException(
                    "ODDB has unsaved changes. Save first or pass discardUnsavedChanges: true.");

            ODDBEditorRuntime.ReloadDatabase();
        }
    }
}
