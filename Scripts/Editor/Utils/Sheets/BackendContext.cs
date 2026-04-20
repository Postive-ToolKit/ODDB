using System.Collections.Generic;

namespace TeamODD.ODDB.Editors.Utils.Sheets
{
    public sealed class BackendContext
    {
        public ExportScope Scope { get; }
        public BackendIntent Intent { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
        public bool Cancelled { get; }

        private BackendContext(
            ExportScope scope,
            BackendIntent intent,
            IReadOnlyDictionary<string, object> parameters,
            bool cancelled)
        {
            Scope = scope;
            Intent = intent;
            Parameters = parameters ?? new Dictionary<string, object>();
            Cancelled = cancelled;
        }

        public static BackendContext Cancel(ExportScope scope, BackendIntent intent)
            => new BackendContext(scope, intent, null, true);

        public static BackendContext Ready(
            ExportScope scope,
            BackendIntent intent,
            IReadOnlyDictionary<string, object> parameters)
            => new BackendContext(scope, intent, parameters, false);

        public T GetParameter<T>(string key, T defaultValue = default)
        {
            if (Parameters != null && Parameters.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return defaultValue;
        }
    }
}
