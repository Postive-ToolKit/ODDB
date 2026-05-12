using System.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Settings;

namespace TeamODD.ODDB.Editors.MCP.Resources
{
    public class DatabaseResource : IMcpResource
    {
        private readonly IODDBEditorUseCase _useCase;
        public DatabaseResource(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string UriOrTemplate => "oddb://database";
        public string Description => "ODDB database metadata (view counts, settings snapshot, MCP port).";
        public string MimeType => "application/json";

        public bool TryMatch(string uri) => uri == UriOrTemplate;

        public object Read(string uri)
        {
            var views = _useCase.GetViews()?.ToList();
            var settings = ODDBSettings.Setting;
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
                    dbPath = settings?.FullDBPath,
                    enableMcp = settings?.EnableMCPServer,
                    mcpHost = settings?.MCPServerHost,
                },
            };
        }
    }
}
