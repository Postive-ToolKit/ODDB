using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.CLI;
using TeamODD.ODDB.Editors.CLI.Resources;
using TeamODD.ODDB.Editors.CLI.Tools;
using TeamODD.ODDB.Editors.CLI.Tools.Data;
using TeamODD.ODDB.Editors.CLI.Tools.Schema;
using TeamODD.ODDB.Editors.CLI.Tools.System;
using TeamODD.ODDB.Editors.Window;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.CLI
{
    /// <summary>Project-local CLI transport. Runs requests on Unity's main thread without a listening port.</summary>
    [InitializeOnLoad]
    internal static class ODDBCliEditorBridge
    {
        private static readonly string Root = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName, "Library", "ODDB", "cli");
        private static readonly string Requests = Path.Combine(Root, "requests");
        private static readonly string Responses = Path.Combine(Root, "responses");
        private static readonly string Heartbeat = Path.Combine(Root, "heartbeat.json");
        private static double _nextScan;
        private static double _nextHeartbeat;

        static ODDBCliEditorBridge()
        {
            Directory.CreateDirectory(Requests);
            Directory.CreateDirectory(Responses);
            EditorApplication.update += Update;
            EditorApplication.quitting += () => { try { File.Delete(Heartbeat); } catch { } };
        }

        private static void Update()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now >= _nextHeartbeat)
            {
                _nextHeartbeat = now + 1;
                WriteAtomic(Heartbeat, JsonConvert.SerializeObject(new
                {
                    pid = System.Diagnostics.Process.GetCurrentProcess().Id,
                    mode = Application.isBatchMode ? "batch" : "editor",
                    updatedUtc = DateTime.UtcNow
                }));
            }
            if (now < _nextScan) return;
            _nextScan = now + 0.1;

            foreach (var path in Directory.GetFiles(Requests, "*.json").OrderBy(p => p))
            {
                var id = Path.GetFileNameWithoutExtension(path);
                var responsePath = Path.Combine(Responses, id + ".json");
                try
                {
                    var request = JObject.Parse(File.ReadAllText(path));
                    if (request.Value<DateTime>("expiresUtc").ToUniversalTime() < DateTime.UtcNow)
                        throw new TimeoutException("CLI request expired before the Editor could process it");
                    var result = Dispatch(request);
                    var operation = request.Value<string>("operation");
                    if (!string.IsNullOrEmpty(operation) && operation.StartsWith("oddb_", StringComparison.Ordinal)
                        && operation != "oddb_save_database" && operation != "oddb_generate_code")
                    {
                        try { CliHistoryStore.Append(Directory.GetParent(Application.dataPath).FullName, operation, request["args"]); }
                        catch (Exception warning) { Debug.LogWarning($"ODDB CLI history: {warning.Message}"); }
                    }
                    WriteAtomic(responsePath, JsonConvert.SerializeObject(new { success = true, result }));
                }
                catch (Exception ex)
                {
                    var code = ex is CliOperationException operationError ? operationError.Kind.ToWireString() : "INTERNAL";
                    WriteAtomic(responsePath, JsonConvert.SerializeObject(new { success = false, error = new { code, message = ex.Message } }));
                }
                finally
                {
                    try { File.Delete(path); } catch { }
                }
            }
        }

        internal static object Dispatch(JObject request)
        {
            var useCase = ODDBEditorRuntime.UseCase;
            if (useCase == null) throw new InvalidOperationException("ODDB editor session is unavailable");
            var uri = request.Value<string>("uri");
            if (!string.IsNullOrEmpty(uri))
            {
                ICliResource[] resources =
                {
                    new DatabaseResource(useCase), new ViewsResource(useCase), new ViewDetailResource(useCase),
                    new TableRowsResource(useCase), new TableInheritedResource(useCase),
                    new BindTypesResource(), new DataTypesResource(), new CommandHistoryResource(useCase)
                };
                var resource = resources.FirstOrDefault(item => item.TryMatch(uri));
                if (resource == null) throw new CliOperationException(CliErrorKind.NotFound, $"Resource not found: {uri}");
                return resource.Read(uri);
            }

            var name = request.Value<string>("operation");
            ICliOperation[] tools =
            {
                new AddRowTool(useCase), new RemoveRowTool(useCase), new SetRowIdTool(useCase), new SetCellTool(useCase),
                new AddViewTool(useCase), new AddTableTool(useCase), new RemoveViewTool(useCase), new RemoveTableTool(useCase),
                new AddFieldTool(useCase), new RemoveFieldTool(useCase), new MoveFieldTool(useCase), new SetFieldTypeTool(useCase),
                new SetViewNameTool(useCase), new SetViewIdTool(useCase), new SetViewBindTypeTool(useCase),
                new SetViewParentTool(useCase), new GenerateCodeTool(useCase), new SaveDatabaseTool(useCase)
            };
            var tool = tools.FirstOrDefault(item => item.Name == name);
            if (tool == null) throw new CliOperationException(CliErrorKind.NotFound, $"Operation not found: {name}");
            return tool.Execute(request["args"] ?? new JObject());
        }

        private static void WriteAtomic(string path, string content)
        {
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, content);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }
    }
}
