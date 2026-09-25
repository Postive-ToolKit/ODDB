using System;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.CLI.Tools.System
{
    public class SaveDatabaseTool : ICliOperation
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
                var session = ODDBEditorRuntime.Session;
                session.Save();
                return new { success = true, path = session.DatabasePath };
            }
            catch (Exception ex)
            {
                throw new CliOperationException(CliErrorKind.SaveFailed, ex.Message);
            }
        }
    }
}
