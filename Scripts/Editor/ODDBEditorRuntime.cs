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
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Settings;
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

        public static IODDBEditorUseCase UseCase
        {
            get
            {
                if (_useCase == null)
                {
                    _useCase = new ODDBEditorUseCase();
                    ODDBEditorDI.RegisterSelfAndInterfaces(_useCase);
                    ODDBEditorDI.RegisterSelfAndInterfaces(_useCase.DataBase);
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
            BootServer();
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;
        }

        private static void BootServer()
        {
            McpMainThread.EnsurePump();

            var settings = ODDBSettings.Setting;
            if (settings == null || !settings.EnableMCPServer)
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

            _dispatcher.Register("initialize", (id, p) => McpResponse.Success(id, new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new { name = "ODDB", version = "1.7.1" },
                capabilities = new { tools = new { }, resources = new { } },
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
    }
}
