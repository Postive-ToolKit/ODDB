using System.Net;
using Newtonsoft.Json;
using TeamODD.ODDB.Editors.MCP;
using TeamODD.ODDB.Editors.MCP.Resources;
using TeamODD.ODDB.Editors.MCP.Tools;
using TeamODD.ODDB.Editors.MCP.Tools.Data;
using TeamODD.ODDB.Editors.MCP.Tools.Schema;
using TeamODD.ODDB.Editors.MCP.Tools.System;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Editors.Settings;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor;

namespace TeamODD.ODDB.Editors
{
    /// <summary>
    /// Editor-process singleton that owns the IODDBEditorUseCase instance and
    /// runs the in-Editor MCP HTTP server. Extracting these out of the window
    /// lets the MCP server and the editor share state so AI-driven mutations
    /// show up in the window immediately.
    /// </summary>
    [InitializeOnLoad]
    public static class ODDBEditorRuntime
    {
        private static IODDBEditorUseCase _useCase;
        private static ODDBMcpServer _server;
        private static McpDispatcher _dispatcher;
        private static McpToolRegistry _toolRegistry;
        private static McpResourceRegistry _resourceRegistry;

        private const string ServerInstructions =
@"This server controls a Unity ODDB database — a hierarchical view/table store
with row data and code-bound entities.

When the user asks to EXPLORE or VIEW data, read resources; do not call tools:
  - Start at oddb://views to list every view and table ({id, name, type, parentId, bindType}).
  - For a specific view's fields, read oddb://views/{id}/schema (or the full
    detail at oddb://views/{id}).
  - For row data, read oddb://tables/{id}/rows. Each cell exposes a raw `value`
    string and a `deserialized` field; prefer `deserialized` when presenting
    values to the user.
  - oddb://bind-types and oddb://data-types describe the available C# bind
    classes and field types respectively.
  - oddb://database returns DB metadata and the active MCP port.
  - oddb://commands/history shows recent undo/redo entries.

When the user asks to MODIFY anything, use tools (names start with `oddb_`):
  data:    oddb_add_row, oddb_remove_row, oddb_set_cell
  schema:  oddb_add_view / oddb_add_table / oddb_remove_view / oddb_remove_table
           oddb_add_field / oddb_remove_field / oddb_move_field
           oddb_set_view_name / oddb_set_view_bind_type / oddb_set_view_parent
  system:  oddb_generate_code, oddb_save_database

All writes route through ODDB's CommandProcessor so each tool call is undoable
via the Editor's Ctrl+Z. After mutations the in-editor table view refreshes
automatically.

Code generation: oddb_generate_code writes C# classes from the current schema,
then Unity compiles asynchronously. The MCP response returns before the
compile finishes; ask the user to wait for the Editor console to settle
before using newly generated types.

Workflow shortcuts you can offer the user:
  - 'Show me the data in <table>' → read oddb://views, find the matching id,
    then read oddb://tables/{id}/rows and render the cells.
  - 'Create a new <name> table' → call oddb_add_table with optional name,
    parentViewId, and bindType in one shot.
  - 'Fill in N rows of <table>' → loop oddb_add_row + oddb_set_cell per cell.";

        public static IODDBEditorUseCase UseCase
        {
            get
            {
                if (_useCase == null)
                {
                    try
                    {
                        var instance = new ODDBEditorUseCase();
                        ODDBEditorDI.RegisterSelfAndInterfaces(instance);
                        ODDBEditorDI.RegisterSelfAndInterfaces(instance.DataBase);
                        _useCase = instance;
                    }
                    catch (System.Exception ex)
                    {
                        McpLog.Warn($"UseCase ctor threw: {ex}");
                        _useCase = null;
                        return null;
                    }
                }
                return _useCase;
            }
        }

        public static IODDatabase Database => UseCase.DataBase;
        public static int? McpPort => _server?.Port;
        public static McpDispatcher Dispatcher => _dispatcher;
        public static McpToolRegistry Tools => _toolRegistry;
        public static McpResourceRegistry Resources => _resourceRegistry;

        static ODDBEditorRuntime()
        {
            // Boot inline. The server itself doesn't touch the use case until
            // a request arrives, so the on-first-run picker UI (which lives
            // inside ODDBEditorUseCase's ctor) only fires when the user first
            // opens the editor window or an MCP tool is called.
            try { BootServer(); }
            catch (System.Exception ex) { McpLog.Error($"BootServer crashed: {ex}"); }
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;
        }

        private static void BootServer()
        {
            McpMainThread.EnsurePump();

            var settings = ODDBEditorSettings.TryLoad();
            if (settings == null)
            {
                McpLog.Lifecycle("ODDBEditorSettings.asset not found — deferring boot. Open the ODDB Editor or run from settings inspector to create.");
                return;
            }
            if (!settings.EnableMCPServer)
            {
                McpLog.Lifecycle("server disabled via settings");
                return;
            }

            // Pre-initialize the use case on this (main) thread so background
            // handlers don't trip Unity's main-thread checks (Resources.Load,
            // ScriptableObject access) on first request. After this, the use
            // case and its database are ready to be read from any thread.
            try { var _ = UseCase; }
            catch (System.Exception ex) { McpLog.Warn($"use case init failed: {ex.Message}"); }

            _dispatcher = new McpDispatcher();
            _toolRegistry = new McpToolRegistry();
            _resourceRegistry = new McpResourceRegistry();

            if (_useCase == null)
            {
                McpLog.Warn("UseCase is null — skipping tool/resource registration. MCP server starts but exposes no endpoints.");
            }
            else
            {
                try
                {
                    // Tools
                    _toolRegistry.Register(new AddRowTool(UseCase));
                    _toolRegistry.Register(new RemoveRowTool(UseCase));
                    _toolRegistry.Register(new SetCellTool(UseCase));
                    _toolRegistry.Register(new AddViewTool(UseCase));
                    _toolRegistry.Register(new AddTableTool(UseCase));
                    _toolRegistry.Register(new RemoveViewTool(UseCase));
                    _toolRegistry.Register(new RemoveTableTool(UseCase));
                    _toolRegistry.Register(new AddFieldTool(UseCase));
                    _toolRegistry.Register(new RemoveFieldTool(UseCase));
                    _toolRegistry.Register(new MoveFieldTool(UseCase));
                    _toolRegistry.Register(new SetFieldTypeTool(UseCase));
                    _toolRegistry.Register(new SetViewNameTool(UseCase));
                    _toolRegistry.Register(new SetViewBindTypeTool(UseCase));
                    _toolRegistry.Register(new SetViewParentTool(UseCase));
                    _toolRegistry.Register(new GenerateCodeTool(UseCase));
                    _toolRegistry.Register(new SaveDatabaseTool(UseCase));

                    // Resources
                    _resourceRegistry.Register(new DatabaseResource(UseCase));
                    _resourceRegistry.Register(new ViewsResource(UseCase));
                    _resourceRegistry.Register(new ViewDetailResource(UseCase));
                    _resourceRegistry.Register(new TableRowsResource(UseCase));
                    _resourceRegistry.Register(new TableInheritedResource(UseCase));
                    _resourceRegistry.Register(new CommandHistoryResource(UseCase));
                    _resourceRegistry.Register(new BindTypesResource());
                    _resourceRegistry.Register(new DataTypesResource());
                }
                catch (System.Exception ex)
                {
                    McpLog.Error($"tool/resource registration failed: {ex}");
                }
            }

            _dispatcher.Register("initialize", (id, p) => McpResponse.Success(id, new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new { name = "ODDB", version = "2.0.21" },
                capabilities = new { tools = new { }, resources = new { } },
                instructions = ServerInstructions,
            }));

            _dispatcher.Register("ping", (id, p) => McpResponse.Success(id, new { }));

            _dispatcher.Register("tools/list", (id, p) => McpResponse.Success(id, new
            {
                tools = _toolRegistry.ListAsJson(),
            }));

            _dispatcher.Register("resources/list", (id, p) => McpResponse.Success(id, new
            {
                resources = _resourceRegistry.ListAsJson(),
            }));

            _dispatcher.Register("tools/call", (id, p) =>
            {
                var name = p?["name"]?.ToString();
                var args = p?["arguments"];
                if (string.IsNullOrEmpty(name))
                    return McpResponse.Failure(id, McpError.InvalidRequest("missing tool name"));
                if (!_toolRegistry.TryGet(name, out var tool))
                    return McpResponse.Failure(id, McpError.MethodNotFound($"tool:{name}"));
                try
                {
                    // Tool handlers touch Unity APIs that must run on the main thread.
                    var result = McpMainThread.Run(() => tool.Execute(args));
                    return McpResponse.Success(id, new
                    {
                        content = new[] { new { type = "text", text = JsonConvert.SerializeObject(result) } },
                    });
                }
                catch (McpException ex)
                {
                    return McpResponse.Failure(id, McpError.Of(ex.Kind, ex.Message, ex.Details));
                }
            });

            _dispatcher.Register("resources/read", (id, p) =>
            {
                var uri = p?["uri"]?.ToString();
                if (string.IsNullOrEmpty(uri))
                    return McpResponse.Failure(id, McpError.InvalidRequest("missing uri"));
                var res = _resourceRegistry.Match(uri);
                if (res == null)
                    return McpResponse.Failure(id, McpError.Of(McpErrorKind.NotFound, $"resource not found: {uri}"));
                try
                {
                    // Resources are read on the background thread. They must avoid
                    // touching Unity-only APIs (ScriptableObject.Setting access is
                    // already cached on the main thread above; database state is
                    // plain C# objects and thread-safe to read).
                    var payload = res.Read(uri);
                    return McpResponse.Success(id, new
                    {
                        contents = new[] { new { uri, mimeType = res.MimeType ?? "application/json", text = JsonConvert.SerializeObject(payload) } },
                    });
                }
                catch (McpException ex)
                {
                    return McpResponse.Failure(id, McpError.Of(ex.Kind, ex.Message, ex.Details));
                }
            });

            int requestedPort = settings.MCPServerPort;
            string host = settings.MCPServerHost;
            for (int i = 0; i < 10; i++)
            {
                int port = requestedPort + i;
                try
                {
                    _server = new ODDBMcpServer();
                    _server.Start(host, port, _dispatcher);
                    McpLog.Lifecycle(i > 0
                        ? $"port {requestedPort} in use, listening on http://{host}:{port}"
                        : $"listening on http://{host}:{port}");
                    return;
                }
                catch (System.Exception ex)
                {
                    _server = null;
                    McpLog.Warn($"bind to {port} failed: {ex.Message}");
                }
            }
            McpLog.Error($"failed to bind any port in [{requestedPort}, {requestedPort + 9}]");
        }

        private static void StopServer()
        {
            _server?.Stop();
            _server = null;
        }

        // For tests only — drops the singleton so the next access rebuilds it.
        internal static void ResetForTesting()
        {
            StopServer();
            _useCase?.Dispose();
            _useCase = null;
        }

        /// <summary>
        /// Drop the in-memory database singleton and force the next UseCase access
        /// to reload from disk. Use after external file changes (git restore, manual
        /// edits, migration tool) to make the editor pick them up without restarting
        /// Unity. Triggers OnViewChanged so live windows refresh.
        /// </summary>
        public static void ReloadDatabase()
        {
            try { _useCase?.Dispose(); }
            catch (System.Exception ex) { McpLog.Warn($"UseCase dispose threw during reload: {ex.Message}"); }
            _useCase = null;
            ODDBEditorDI.DisposeAll();

            // Re-trigger lazy construction immediately so DI is repopulated and
            // any open windows can re-resolve their dependencies.
            var _ = UseCase;
            McpLog.Lifecycle("database reloaded from disk");
        }
    }
}
