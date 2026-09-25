using System.Text.RegularExpressions;
using System.Security.Cryptography;
using TeamODD.ODDB.Editors.Commands;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Cli;

internal sealed class CliDatabaseSession : IODDBEditorUseCase, IDisposable
{
    private readonly ODDatabase _database;
    private readonly List<ICommand> _history = new();
    private readonly FileStream _lock;
    private readonly byte[] _originalHash;
    private bool _dirty;

    public string ProjectPath { get; }
    public string DatabasePath { get; }
    public ODDatabase Database => _database;
    public IReadOnlyList<ICommand> History => _history;
    public bool IsDirty => _dirty;

    public CliDatabaseSession(string projectPath, string overrideDbPath = null)
    {
        ProjectPath = Path.GetFullPath(projectPath);
        if (!Directory.Exists(Path.Combine(ProjectPath, "Assets")))
            throw new ArgumentException($"Not a Unity project: {ProjectPath}");

        DatabasePath = overrideDbPath == null
            ? ResolveDatabasePath(ProjectPath)
            : Path.GetFullPath(overrideDbPath);
        if (!File.Exists(DatabasePath))
            throw new FileNotFoundException("ODDB database does not exist; refusing to create an empty replacement", DatabasePath);
        _originalHash = SHA256.HashData(File.ReadAllBytes(DatabasePath));

        var lockDirectory = Path.Combine(ProjectPath, "Library", "ODDB");
        Directory.CreateDirectory(lockDirectory);
        _lock = new FileStream(Path.Combine(lockDirectory, "cli.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);

        try
        {
            if (!ODDatabase.TryLoad(DatabasePath, out _database, out var report)
                || _database == null || (report != null && !report.IsSafeToSave))
                throw new InvalidDataException($"ODDB load is unsafe: {report?.FailureStage}: {report?.FailureReason}");
        }
        catch
        {
            _lock.Dispose();
            throw;
        }
    }

    public static string ResolveDatabasePath(string projectPath)
    {
        var resources = Path.Combine(projectPath, "Assets", "Resources");
        var settingsPath = new[] { "ODDBRuntimeSettings.asset", "ODDBSettings.asset" }
            .Select(name => Path.Combine(resources, name)).FirstOrDefault(File.Exists);
        if (settingsPath == null)
            return Path.Combine(resources, "ODDB.bytes");

        var yaml = File.ReadAllText(settingsPath);
        var dbName = ReadUnityScalar(yaml, "_dbName");
        if (string.IsNullOrWhiteSpace(dbName)) dbName = "ODDB.bytes";
        var configured = ReadUnityScalar(yaml, "_dbPath");
        if (string.IsNullOrWhiteSpace(configured))
            return Path.Combine(resources, dbName);

        configured = configured.Replace('\\', '/');
        string folder;
        if (configured.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            folder = Path.Combine(projectPath, "Assets");
        else if (configured.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            folder = Path.Combine(projectPath, configured.Replace('/', Path.DirectorySeparatorChar));
        else if (configured.StartsWith("//", StringComparison.Ordinal)
                 || (configured.Length >= 3 && char.IsLetter(configured[0]) && configured[1] == ':' && configured[2] == '/')
                 || (configured.StartsWith("/", StringComparison.Ordinal) && Directory.Exists(configured)))
            folder = configured;
        else
            folder = Path.Combine(projectPath, "Assets", configured.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        return Path.GetFullPath(Path.Combine(folder, dbName));
    }

    private static string ReadUnityScalar(string yaml, string key)
    {
        var match = Regex.Match(yaml, @"(?m)^[ \t]*" + Regex.Escape(key) + @":[ \t]*(?<value>[^\r\n]*)");
        var value = match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            value = value[1..^1];
        return value;
    }

    public void Save()
    {
        if (!_dirty) return;
        if (CliEditorClient.IsActive(ProjectPath) || CliEditorClient.ProjectMayBeOpen(ProjectPath))
            throw new IOException("Unity opened this project while the CLI command was running; save refused");
        if (!SHA256.HashData(File.ReadAllBytes(DatabasePath)).SequenceEqual(_originalHash))
            throw new IOException("ODDB database changed after the CLI loaded it; save refused");
        var directory = Path.GetDirectoryName(DatabasePath)!;
        var temporary = Path.Combine(directory, $".{Path.GetFileName(DatabasePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            _database.Save(temporary);
            // Keep the pre-change bytes even if the replacement fails.
            var backup = DatabasePath + ".precli.bak";
            File.Copy(DatabasePath, backup, true);
            File.Move(temporary, DatabasePath, true);
            _dirty = false;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public void Dispose() => _lock?.Dispose();

    private void Execute(ICommand command)
    {
        command.ExecutionTime = DateTime.Now;
        command.Execute();
        _history.Add(command);
        _dirty = true;
    }

    public IEnumerable<IView> GetViews(Predicate<IView> predicate = null) =>
        predicate == null ? _database.GetAll() : _database.GetAll().Where(view => predicate(view));

    public IView GetViewByKey(string key) => string.IsNullOrEmpty(key) ? null : _database.GetView(new ODDBID(key));
    public ODDBViewType GetViewTypeByKey(string key) => GetViewByKey(key) switch
    {
        Table => ODDBViewType.Table,
        View => ODDBViewType.View,
        _ => ODDBViewType.None
    };

    public void AddTable() => Execute(new AddViewItemCommand(_database.Tables, "Add Table", null));
    public void AddView() => Execute(new AddViewItemCommand(_database.Views, "Add View", null));
    public void RemoveTable(string tableId) => Execute(new RemoveViewItemCommand(_database.Tables, new ODDBID(tableId), "Remove Table", null));
    public void RemoveView(string viewId) => Execute(new RemoveViewItemCommand(_database.Views, new ODDBID(viewId), "Remove View", null));
    public void AddRow(string tableId) => Execute(new AddRowCommand((Table)GetViewByKey(tableId), null));
    public void RemoveRow(string tableId, string rowId) => Execute(new RemoveRowCommand((Table)GetViewByKey(tableId), rowId, null));
    public void SetRowId(string tableId, string rowId, string newRowId) => Execute(new SetRowIdCommand((Table)GetViewByKey(tableId), rowId, newRowId, null));
    public Row GetRow(string rowId) => _database.Tables.GetAll().OfType<Table>().Select(table => table.GetRow(rowId)).FirstOrDefault(row => row != null);
    public void SetCellData(string tableId, string rowId, int fieldIndex, object value, bool direct = false) =>
        Execute(new SetCellDataCommand((Table)GetViewByKey(tableId), rowId, fieldIndex, value, null, direct));
    public void AddField(string viewId, Field field) => Execute(new AddFieldCommand(GetViewByKey(viewId), field, null));
    public void RemoveField(string viewId, int index) => Execute(new RemoveFieldCommand(GetViewByKey(viewId), index, null));
    public void MoveField(string viewId, int oldIndex, int newIndex) => Execute(new MoveFieldCommand(GetViewByKey(viewId), oldIndex, newIndex, null));
    public void SetFieldType(string viewId, int fieldIndex, string typeKey, string param) =>
        Execute(new SetFieldTypeCommand(GetViewByKey(viewId), fieldIndex, typeKey, param, null));
    public void SetViewName(string viewId, string name) => Execute(new SetViewNameCommand(GetViewByKey(viewId), name, null));
    public void SetViewId(string viewId, string newViewId)
    {
        var view = GetViewByKey(viewId);
        var repo = view is Table ? _database.Tables : _database.Views;
        Execute(new SetViewIdCommand(repo, new ODDBID(viewId), new ODDBID(newViewId), null));
    }
    public void SetViewBindType(string viewId, Type type) => Execute(new SetBindTypeCommand(GetViewByKey(viewId), type, null));
    public void SetViewParent(string viewId, string parentId) => Execute(new SetParentCommand(GetViewByKey(viewId), GetViewByKey(parentId), null));
    public IEnumerable<Table> GetInheritedTables(string viewId)
    {
        var children = _database.GetAll().Where(view => view?.ParentView?.ID != null)
            .GroupBy(view => view.ParentView.ID.ToString())
            .ToDictionary(group => group.Key, group => group.ToList());
        var seen = new HashSet<string>();
        var result = new List<Table>();
        void Collect(IView view)
        {
            if (view == null || !seen.Add(view.ID.ToString())) return;
            if (view is Table table) result.Add(table);
            if (children.TryGetValue(view.ID.ToString(), out var descendants))
                foreach (var descendant in descendants) Collect(descendant);
        }
        Collect(GetViewByKey(viewId));
        return result;
    }
    public IEnumerable<ICommand> GetUndoHistory() => _history.AsEnumerable().Reverse();
    public IEnumerable<ICommand> GetRedoHistory() => Array.Empty<ICommand>();
}
