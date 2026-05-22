using System;
using System.IO;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Settings;

namespace TeamODD.ODDB.Editors.MCP.Tools.System
{
    public class SaveDatabaseTool : IMcpTool
    {
        private readonly IODDBEditorUseCase _useCase;
        public SaveDatabaseTool(IODDBEditorUseCase useCase) => _useCase = useCase;

        public string Name => "oddb_save_database";
        public string Description => "Persist the current ODDB database to disk.";
        public JObject InputSchema => new JObject { ["type"] = "object" };

        public object Execute(JToken args)
        {
            try
            {
                var s = ODDBRuntimeSettings.Setting;
                var fullPath = Path.Combine(s.Path, s.DBName);
                _useCase.SaveDatabase(fullPath);
                return new { success = true, path = fullPath };
            }
            catch (Exception ex)
            {
                throw new McpException(McpErrorKind.SaveFailed, ex.Message);
            }
        }
    }
}
