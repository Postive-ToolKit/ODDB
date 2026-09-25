using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using TeamODD.ODDB.Editors.CLI;
using TeamODD.ODDB.Editors.CLI.Resources;
using TeamODD.ODDB.Editors.CLI.Tools;
using TeamODD.ODDB.Editors.CLI.Tools.Data;
using TeamODD.ODDB.Editors.CLI.Tools.Schema;

namespace TeamODD.ODDB.Cli;

internal static class Program
{
    private static readonly Dictionary<string, string> ToolNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rows add"] = "oddb_add_row", ["rows remove"] = "oddb_remove_row", ["rows set-id"] = "oddb_set_row_id",
        ["cells set"] = "oddb_set_cell", ["views add"] = "oddb_add_view", ["views remove"] = "oddb_remove_view",
        ["tables add"] = "oddb_add_table", ["tables remove"] = "oddb_remove_table",
        ["fields add"] = "oddb_add_field", ["fields remove"] = "oddb_remove_field",
        ["fields move"] = "oddb_move_field", ["fields set-type"] = "oddb_set_field_type",
        ["views set-name"] = "oddb_set_view_name", ["views set-id"] = "oddb_set_view_id",
        ["views set-bind-type"] = "oddb_set_view_bind_type", ["views set-parent"] = "oddb_set_view_parent",
        ["code generate"] = "oddb_generate_code", ["database save"] = "oddb_save_database",
    };

    private static int Main(string[] commandLine)
    {
        var json = commandLine.Contains("--json");
        try
        {
            var (words, options) = Parse(commandLine);
            if (words.Count == 0 || words[0] is "help" or "--help" or "-h")
            {
                Console.WriteLine(Help);
                return 0;
            }
            if (words[0] is "--version" or "version")
            {
                Console.WriteLine("ODDB CLI 2.9.0");
                return 0;
            }
            if (!options.TryGetValue("project", out var projectToken) || string.IsNullOrWhiteSpace(projectToken?.ToString()))
                throw new ArgumentException("--project <Unity project path> is required");

            var project = projectToken.ToString();
            var databaseOverride = options.TryGetValue("db", out var dbToken) ? dbToken?.ToString() : null;
            var command = string.Join(' ', words.Take(2));
            var payload = options.TryGetValue("args", out var arguments)
                ? JObject.Parse(arguments.ToString())
                : BuildArguments(options);

            var uri = ResolveUri(words, command, payload);
            var operation = words[0] == "call" ? words.ElementAtOrDefault(1)
                : ToolNames.TryGetValue(command, out var mapped) ? mapped : null;
            var editorIsActive = CliEditorClient.IsActive(project);
            var requiresUnity = RequiresUnity(operation, uri, payload);
            if (databaseOverride != null && (editorIsActive || requiresUnity))
                throw new ArgumentException("--db can only be used for standalone Core operations");
            if (editorIsActive)
            {
                var live = CliEditorClient.Execute(project, operation, uri, payload);
                Console.WriteLine(JsonConvert.SerializeObject(live, json ? Formatting.None : Formatting.Indented));
                return 0;
            }
            if (CliEditorClient.ProjectMayBeOpen(project))
                throw new IOException("Unity appears to have this project open but its CLI bridge is unavailable; refusing an offline write");

            if (requiresUnity)
            {
                var unityPath = options.TryGetValue("unity", out var pathToken) ? pathToken?.ToString() : null;
                var batch = CliUnityBatchClient.Execute(project, operation, uri, payload, unityPath);
                Console.WriteLine(JsonConvert.SerializeObject(batch, json ? Formatting.None : Formatting.Indented));
                return 0;
            }

            using var session = new CliDatabaseSession(project, databaseOverride);
            object result;
            if (words[0] == "read")
            {
                if (words.Count < 2) throw new ArgumentException("read requires an oddb:// URI");
                result = Read(session, words[1]);
            }
            else if (words[0] == "call")
            {
                if (words.Count < 2) throw new ArgumentException("call requires an oddb_* operation name");
                result = Call(session, words[1], payload);
            }
            else if (ToolNames.TryGetValue(command, out var toolName))
                result = toolName is "oddb_generate_code" or "oddb_save_database"
                    ? Command(session, command, payload)
                    : Call(session, toolName, payload);
            else
                result = Command(session, command, payload);

            if (session.IsDirty) session.Save();
            if (session.History.Count > 0)
            {
                try { CliHistoryStore.Append(session.ProjectPath, operation ?? command, payload); }
                catch (Exception warning) { Console.Error.WriteLine($"ODDB history warning: {warning.Message}"); }
            }
            Console.WriteLine(JsonConvert.SerializeObject(result, json ? Formatting.None : Formatting.Indented));
            return 0;
        }
        catch (Exception exception)
        {
            var kind = exception is CliOperationException operationError ? operationError.Kind.ToWireString() : exception switch
            {
                CliRemoteException remote => remote.Code,
                ArgumentException => "INVALID_ARG", FileNotFoundException => "NOT_FOUND",
                InvalidDataException => "LOAD_FAILED", IOException => "IO_ERROR", _ => "INTERNAL"
            };
            Console.Error.WriteLine(json
                ? JsonConvert.SerializeObject(new { success = false, error = new { code = kind, message = exception.Message } })
                : $"ODDB {kind}: {exception.Message}");
            return kind switch { "INVALID_ARG" => 2, "NOT_FOUND" => 3, "CONFLICT" => 4, _ => 1 };
        }
    }

    private static object Command(CliDatabaseSession session, string command, JObject args)
    {
        switch (command.ToLowerInvariant())
        {
            case "views list": return Read(session, "oddb://views");
            case "views pure": return Read(session, "oddb://views/pure");
            case "views show": return Read(session, "oddb://views/" + Required(args, "viewId"));
            case "views schema": return Read(session, "oddb://views/" + Required(args, "viewId") + "/schema");
            case "rows list": return Read(session, "oddb://tables/" + Required(args, "tableId") + "/rows");
            case "rows show": return Read(session, "oddb://tables/" + Required(args, "tableId") + "/rows/" + Required(args, "rowId"));
            case "tables inherited": return Read(session, "oddb://tables/" + Required(args, "viewId") + "/inherited");
            case "types data": return Read(session, "oddb://data-types");
            case "types bind": return Read(session, "oddb://bind-types");
            case "history list": return CliHistoryStore.Read(session.ProjectPath);
            case "database info": return Read(session, "oddb://database");
            case "database save": return new { success = true, path = session.DatabasePath, changed = false };
            case "code generate": throw new InvalidOperationException("Code generation without an open Unity Editor is not implemented yet");
            default: throw new ArgumentException($"Unknown command: {command}");
        }
    }

    private static string Required(JObject args, string key) => args[key]?.ToString() is { Length: > 0 } value
        ? value : throw new ArgumentException($"--{ToKebab(key)} is required");

    private static string ResolveUri(List<string> words, string command, JObject args)
    {
        if (words[0] == "read") return words.ElementAtOrDefault(1);
        return command.ToLowerInvariant() switch
        {
            "views list" => "oddb://views",
            "views pure" => "oddb://views/pure",
            "views show" => "oddb://views/" + Required(args, "viewId"),
            "views schema" => "oddb://views/" + Required(args, "viewId") + "/schema",
            "rows list" => "oddb://tables/" + Required(args, "tableId") + "/rows",
            "rows show" => "oddb://tables/" + Required(args, "tableId") + "/rows/" + Required(args, "rowId"),
            "tables inherited" => "oddb://tables/" + Required(args, "viewId") + "/inherited",
            "types data" => "oddb://data-types",
            "types bind" => "oddb://bind-types",
            "history list" => "oddb://commands/history",
            "database info" => "oddb://database",
            _ => null
        };
    }

    private static bool RequiresUnity(string operation, string uri, JObject args)
    {
        if (uri != null)
            return (uri.StartsWith("oddb://tables/", StringComparison.Ordinal)
                    && uri.Contains("/rows", StringComparison.Ordinal))
                   || uri.StartsWith("oddb://views/", StringComparison.Ordinal)
                   || uri is "oddb://bind-types" or "oddb://data-types";

        if (operation == null) return false;
        if (operation is "oddb_generate_code" or "oddb_add_row" or "oddb_set_view_bind_type" or "oddb_set_view_id"
            or "oddb_set_view_parent" or "oddb_set_cell" or "oddb_add_field" or "oddb_set_field_type")
            return true;
        if (operation == "oddb_add_table" && args["bindType"] != null)
            return true;
        return false;
    }

    private static object Call(CliDatabaseSession session, string name, JObject args)
    {
        if (name == "oddb_save_database")
            return new { success = true, path = session.DatabasePath, changed = false };
        ICliOperation[] tools =
        {
            new AddRowTool(session), new RemoveRowTool(session), new SetRowIdTool(session), new SetCellTool(session),
            new AddViewTool(session), new AddTableTool(session), new RemoveViewTool(session), new RemoveTableTool(session),
            new AddFieldTool(session), new RemoveFieldTool(session), new MoveFieldTool(session), new SetFieldTypeTool(session),
            new SetViewNameTool(session), new SetViewIdTool(session), new SetViewBindTypeTool(session), new SetViewParentTool(session)
        };
        var tool = tools.FirstOrDefault(candidate => candidate.Name == name);
        if (tool == null) throw new ArgumentException($"Unknown operation: {name}");
        return tool.Execute(args);
    }

    private static object Read(CliDatabaseSession session, string uri)
    {
        if (uri == "oddb://commands/history") return CliHistoryStore.Read(session.ProjectPath);
        if (uri == "oddb://database") return new
        {
            viewCount = session.Database.Views.Count,
            tableCount = session.Database.Tables.Count,
            settings = new { dbPath = session.DatabasePath }
        };
        ICliResource[] resources =
        {
            new ViewsResource(session), new ViewDetailResource(session), new TableRowsResource(session),
            new TableInheritedResource(session), new BindTypesResource(), new DataTypesResource(),
            new CommandHistoryResource(session)
        };
        var resource = resources.FirstOrDefault(candidate => candidate.TryMatch(uri));
        if (resource == null) throw new ArgumentException($"Unknown resource URI: {uri}");
        return resource.Read(uri);
    }

    private static (List<string> words, Dictionary<string, JToken> options) Parse(string[] args)
    {
        var words = new List<string>();
        var options = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal)) { words.Add(arg); continue; }
            var name = arg[2..];
            if (name == "json") { options[name] = true; continue; }
            if (i + 1 >= args.Length) throw new ArgumentException($"{arg} needs a value");
            options[name] = args[++i];
        }
        return (words, options);
    }

    private static JObject BuildArguments(Dictionary<string, JToken> options)
    {
        var payload = new JObject();
        foreach (var (key, token) in options)
        {
            if (key is "project" or "db" or "json" or "args" or "unity") continue;
            var camel = ToCamel(key);
            var raw = token.ToString();
            payload[camel] = key is "field-index" or "old-index" or "new-index" or "index"
                ? int.Parse(raw)
                : key is "value" or "type-name" or "parent-view-id" or "bind-type" or "view-ids"
                    ? TryJson(raw)
                    : raw;
        }
        return payload;
    }

    private static JToken TryJson(string raw)
    {
        try { return JToken.Parse(raw); }
        catch (JsonReaderException) { return raw; }
    }

    private static string ToCamel(string value)
    {
        var parts = value.Split('-');
        return parts[0] + string.Concat(parts.Skip(1).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
    private static string ToKebab(string value) => Regex.Replace(value, "[A-Z]", match => "-" + match.Value.ToLowerInvariant());

    private const string Help = """
ODDB CLI
  oddb --project <Unity project> <group> <action> [--name value ...] [--json]
  oddb --project <Unity project> read oddb://views [--json]
  oddb --project <Unity project> call oddb_add_table --args '{"name":"Items"}' --json

Groups: database, views, tables, rows, cells, fields, types, history.
Run with --json for machine-readable output. Standalone mutations save automatically.
Use database save after mutations sent to an open Unity Editor.
""";
}
