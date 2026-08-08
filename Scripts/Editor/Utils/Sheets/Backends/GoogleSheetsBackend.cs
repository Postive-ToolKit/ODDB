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

            var editorSettings = ODDBEditorSettings.Setting;
            if (string.IsNullOrWhiteSpace(editorSettings.GoogleSpreadsheetId))
            {
                ShowSettingsRequired(
                    editorSettings,
                    "Google Spreadsheet ID is not configured. Enter the Spreadsheet ID, then test the connection.");
                return Task.FromResult(BackendContext.Cancel(scope, intent));
            }

            if (!editorSettings.HasGoogleOAuthClientConfiguration)
            {
                ShowSettingsRequired(
                    editorSettings,
                    "Google OAuth Desktop Client credentials are required. Enter and save the Client ID and Client Secret.");
                return Task.FromResult(BackendContext.Cancel(scope, intent));
            }

            if (!GoogleSheetsUserSettings.HasStoredAuthorization)
            {
                ShowSettingsRequired(
                    editorSettings,
                    "Google authorization is required. Sign in with Google, then test the connection.");
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
            // Grouped physical tabs use their root View ID, so scope is applied only
            // after the use case unpacks them back into table-level SheetInfo values.
            var filtered = FilterSheets(sheets, ExportScope.EntireDatabase);
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
            // The use case already selected the physical sheet(s) for this export.
            var filtered = FilterSheets(sheets, ExportScope.EntireDatabase);
            ReportSheets(progress, filtered, "Preparing sheet for upload", 0.15f, 0.45f);
            ReportStage(progress, $"Uploading {filtered.Count} sheet(s) to Google Sheets...", 0.5f);
            await ODDBGoogleSheetUtility.SaveSheetsAsync(filtered, progress, ct);
            ReportStage(progress, "Finalizing...", 0.95f);
        }

        private static void ReportStage(IProgress<float> progress, string stage, float value)
        {
            ODDBProgress.Report(progress, stage, value);
        }

        private static void ShowSettingsRequired(ODDBEditorSettings settings, string message)
        {
            EditorUtility.DisplayDialog(
                "Google Sheets Setup Required",
                message + "\n\nODDBEditorSettings will now be selected in the Inspector.",
                "Open Settings");

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
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
