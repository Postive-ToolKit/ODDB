using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets;
using TeamODD.ODDB.Runtime.Settings;
using UnityEditor;

namespace TeamODD.ODDB.Editors.Utils.Sheets.Backends
{
    public sealed class GoogleSheetsBackend : ISheetBackend
    {
        public string DisplayName => "Google Sheets";
        public bool SupportsPartial => true;

        public Task<BackendContext> PrepareAsync(ExportScope scope, BackendIntent intent)
        {
            if (ODDBSettings.Setting == null || !ODDBSettings.Setting.IsInitialized)
            {
                EditorUtility.DisplayDialog(
                    "Google Sheets",
                    "ODDBSettings is not initialized. Open ODDB Editor once before using Google Sheets backend.",
                    "OK");
                return Task.FromResult(BackendContext.Cancel(scope, intent));
            }

            if (string.IsNullOrEmpty(ODDBSettings.Setting.GoogleSheetAPIURL))
            {
                EditorUtility.DisplayDialog(
                    "Google Sheets",
                    "GoogleSheetAPIURL is not configured in ODDBSettings. Configure it before using the Google Sheets backend.",
                    "OK");
                return Task.FromResult(BackendContext.Cancel(scope, intent));
            }

            return Task.FromResult(
                BackendContext.Ready(scope, intent, new Dictionary<string, object>()));
        }

        public async Task<IReadOnlyList<SheetInfo>> LoadAsync(
            BackendContext ctx,
            IProgress<float> progress,
            CancellationToken ct)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            progress?.Report(0.1f);
            var sheets = await GoogleSheetUtility.LoadSheetsAsync(ct);
            progress?.Report(0.9f);
            var filtered = FilterSheets(sheets, ctx.Scope);
            progress?.Report(1f);
            return filtered;
        }

        public async Task SaveAsync(
            BackendContext ctx,
            IReadOnlyList<SheetInfo> sheets,
            IProgress<float> progress,
            CancellationToken ct)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (sheets == null) throw new ArgumentNullException(nameof(sheets));

            progress?.Report(0.1f);
            var filtered = FilterSheets(sheets, ctx.Scope);
            progress?.Report(0.4f);
            await GoogleSheetUtility.SaveSheetsAsync(filtered, ct);
            progress?.Report(1f);
        }

        private static IReadOnlyList<SheetInfo> FilterSheets(IReadOnlyList<SheetInfo> sheets, ExportScope scope)
        {
            if (scope.All) return sheets;

            var matches = new List<SheetInfo>();
            foreach (var sheet in sheets)
            {
                if (sheet != null && sheet.ID == scope.TargetTableId)
                    matches.Add(sheet);
            }
            return matches;
        }
    }
}
