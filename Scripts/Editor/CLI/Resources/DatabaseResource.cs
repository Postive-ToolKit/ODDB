using System.Linq;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Settings;

namespace TeamODD.ODDB.Editors.CLI.Resources
{
    public class DatabaseResource : ICliResource
    {
        private readonly IODDBEditorUseCase _useCase;
        private readonly string _dbPath;

        public DatabaseResource(IODDBEditorUseCase useCase)
        {
            _useCase = useCase;
            var runtime = ODDBRuntimeSettings.Setting;
            _dbPath = runtime?.FullDBPath;
        }

        public string UriOrTemplate => "oddb://database";
        public string Description => "ODDB database metadata and path.";
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
                settings = new
                {
                    dbPath = _dbPath,
                },
            };
        }
    }
}
