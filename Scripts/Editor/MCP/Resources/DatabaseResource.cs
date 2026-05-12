using System.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Settings;

namespace TeamODD.ODDB.Editors.MCP.Resources
{
    public class DatabaseResource : IMcpResource
    {
        private readonly IODDBEditorUseCase _useCase;
        // Settings are cached at construction (which happens on the main thread
        // during BootServer). Background-thread reads must not call
        // ODDBSettings.Setting since it invokes Resources.Load.
        private readonly string _dbPath;
        private readonly bool _enableMcp;
        private readonly string _mcpHost;

        public DatabaseResource(IODDBEditorUseCase useCase)
        {
            _useCase = useCase;
            var s = ODDBSettings.Setting;
            _dbPath = s?.FullDBPath;
            _enableMcp = s?.EnableMCPServer ?? false;
            _mcpHost = s?.MCPServerHost;
        }

        public string UriOrTemplate => "oddb://database";
        public string Description => "ODDB database metadata (view counts, settings snapshot, MCP port).";
        public string MimeType => "application/json";

        public bool TryMatch(string uri) => uri == UriOrTemplate;

        public object Read(string uri)
        {
            var views = _useCase.GetViews()?.ToList();
            int viewCount = 0, tableCount = 0;
            if (views != null)
            {
                foreach (var v in views)
                {
                    var t = _useCase.GetViewTypeByKey(v.ID);
                    if (t == ODDBViewType.View) viewCount++;
                    else if (t == ODDBViewType.Table) tableCount++;
                }
            }
            return new
            {
                viewCount,
                tableCount,
                mcpPort = ODDBEditorRuntime.McpPort,
                settings = new
                {
                    dbPath = _dbPath,
                    enableMcp = _enableMcp,
                    mcpHost = _mcpHost,
                },
            };
        }
    }
}
