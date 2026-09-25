using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.CLI;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.CLI
{
    /// <summary>Executes Unity-dependent CLI operations when the project is not already open.</summary>
    public static class ODDBCliBatchRunner
    {
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-oddbCliRequest");
            if (index < 0 || index + 2 >= args.Length)
                throw new ArgumentException("-oddbCliRequest <request> <response> is required");

            var requestPath = args[index + 1];
            var responsePath = args[index + 2];
            try
            {
                var request = JObject.Parse(File.ReadAllText(requestPath));
                var result = ODDBCliEditorBridge.Dispatch(request);
                var operation = request.Value<string>("operation");
                if (!string.IsNullOrEmpty(operation) && operation.StartsWith("oddb_", StringComparison.Ordinal)
                    && operation != "oddb_save_database" && operation != "oddb_generate_code")
                {
                    ODDBEditorRuntime.Session.Save();
                    try { CliHistoryStore.Append(Directory.GetParent(Application.dataPath).FullName, operation, request["args"]); }
                    catch (Exception warning) { Debug.LogWarning($"ODDB CLI history: {warning.Message}"); }
                }
                File.WriteAllText(responsePath, JsonConvert.SerializeObject(new { success = true, result }));
            }
            catch (Exception ex)
            {
                var code = ex is CliOperationException operationError ? operationError.Kind.ToWireString() : "INTERNAL";
                File.WriteAllText(responsePath, JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = new { code, message = ex.Message }
                }));
                EditorApplication.Exit(1);
            }
        }
    }
}
