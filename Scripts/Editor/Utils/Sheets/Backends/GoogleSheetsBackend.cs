using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Editors.UI.Progress;
using TeamODD.ODDB.Editors.Settings;
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
            if (ODDBRuntimeSettings.Setting == null || !ODDBRuntimeSettings.Setting.IsInitialized)
            {
                EditorUtility.DisplayDialog(
                    "Google Sheets",
                    "ODDBRuntimeSettings is not initialized. Open ODDB Editor once before using Google Sheets backend.",
                    "OK");
                return Task.FromResult(BackendContext.Cancel(scope, intent));
            }

            if (string.IsNullOrWhiteSpace(ODDBEditorSettings.Setting.GoogleSpreadsheetId))
            {
                EditorUtility.DisplayDialog(
                    "Google Sheets",
                    "Google Spreadsheet ID is not configured in ODDBEditorSettings.",
                    "OK");
                return Task.FromResult(BackendContext.Cancel(scope, intent));
            }

            if (!GoogleSheetsUserSettings.TryGetCredentialPath(out _, out var failureReason))
            {
                EditorUtility.DisplayDialog("Google Sheets", failureReason, "OK");
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

            ReportStage(progress, "Loading data from Google Sheets...", 0.1f);
            var sheets = await ODDBGoogleSheetUtility.LoadSheetsAsync(ctx.Scope, ct);
            ReportStage(progress, "Parsing sheet data...", 0.7f);
            var filtered = FilterSheets(sheets, ctx.Scope);
            ReportSheets(progress, filtered, "Processing downloaded sheet", 0.75f, 0.95f);
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

            ReportStage(progress, "Preparing sheets for export...", 0.1f);
            var filtered = FilterSheets(sheets, ctx.Scope);
            ReportSheets(progress, filtered, "Preparing sheet for upload", 0.15f, 0.45f);
            ReportStage(progress, $"Uploading {filtered.Count} sheet(s) to Google Sheets...", 0.5f);
            await ODDBGoogleSheetUtility.SaveSheetsAsync(filtered, progress, ct);
            ReportStage(progress, "Finalizing...", 0.95f);
        }

        private static void ReportStage(IProgress<float> progress, string stage, float value)
        {
            ODDBProgress.Report(progress, stage, value);
        }

        private static void ReportSheets(
            IProgress<float> progress,
            IReadOnlyList<SheetInfo> sheets,
            string action,
            float start,
            float end)
        {
            if (sheets == null || sheets.Count == 0)
            {
                ReportStage(progress, $"{action}: no sheets.", end);
                return;
            }

            for (var i = 0; i < sheets.Count; i++)
            {
                var sheetName = string.IsNullOrEmpty(sheets[i]?.Name) ? sheets[i]?.ID : sheets[i].Name;
                if (string.IsNullOrEmpty(sheetName))
                    sheetName = "Unnamed sheet";

                var t = (i + 1f) / sheets.Count;
                var value = start + (end - start) * t;
                ReportStage(progress, $"{action} ({i + 1}/{sheets.Count}): {sheetName}", value);
            }
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
