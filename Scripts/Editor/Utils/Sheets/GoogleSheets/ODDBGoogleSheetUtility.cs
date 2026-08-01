using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Editors.UI.Progress;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    public static class ODDBGoogleSheetUtility
    {
        public static Task<List<SheetInfo>> LoadSheetsAsync(ExportScope scope, CancellationToken ct)
        {
            return new GoogleSheetsSyncService().LoadAsync(scope, ct);
        }

        public static Task SaveSheetsAsync(
            IReadOnlyList<SheetInfo> sheets,
            IProgress<float> progress,
            CancellationToken ct)
        {
            return new GoogleSheetsSyncService().SaveAsync(sheets, progress, ct);
        }

        [MenuItem(GoogleSheetConfig.MENU_ROOT + "Import from Google Sheets")]
        public static async void LoadFromGoogleSheet()
        {
            var useCase = ODDBEditorRuntime.UseCase;
            if (useCase == null)
            {
                Debug.LogError("ODDB Editor is not initialized. Open the ODDB Editor and try again.");
                return;
            }

            using (var progress = ODDBProgressScope.Show("ODDB Google Sheets Import", "Preparing import...", 0f))
            {
                try
                {
                    await useCase.ImportAsync(ExportScope.EntireDatabase, new Backends.GoogleSheetsBackend(), progress);
                    await progress.ShowResultAsync("Google Sheets import completed.", false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    await progress.ShowResultAsync("Google Sheets import failed: " + e.Message, true);
                }
            }
        }

        [MenuItem(GoogleSheetConfig.MENU_ROOT + "Export to Google Sheets")]
        public static async void SaveToGoogleSheet()
        {
            var useCase = ODDBEditorRuntime.UseCase;
            if (useCase == null)
            {
                Debug.LogError("ODDB Editor is not initialized. Open the ODDB Editor and try again.");
                return;
            }

            using (var progress = ODDBProgressScope.Show("ODDB Google Sheets Export", "Preparing export...", 0f))
            {
                try
                {
                    await useCase.ExportAsync(ExportScope.EntireDatabase, new Backends.GoogleSheetsBackend(), progress);
                    await progress.ShowResultAsync("Google Sheets export completed.", false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    await progress.ShowResultAsync("Google Sheets export failed: " + e.Message, true);
                }
            }
        }
    }
}
