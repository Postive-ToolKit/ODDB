using System;
using TeamODD.ODDB.Editors.UI.Progress;
using TeamODD.ODDB.Editors.Utils.Sheets;
using TeamODD.ODDB.Editors.Utils.Sheets.Backends;
using TeamODD.ODDB.Editors.Window;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI.Menus
{
    internal static class ODDBImportExportMenu
    {
        public static void BuildToolbarExportMenu(ToolbarMenu menu, IODDBEditorUseCase useCase)
        {
            menu.menu.AppendAction("CSV/All Tables",
                _ => RunExport(useCase, ExportScope.EntireDatabase, new CsvSheetBackend()));
            menu.menu.AppendAction("CSV/Selected Table",
                _ => RunExportSelected(useCase, new CsvSheetBackend()),
                _ => SelectedStatus(useCase));
            menu.menu.AppendAction("Google Sheets/All Tables",
                _ => RunExport(useCase, ExportScope.EntireDatabase, new GoogleSheetsBackend()));
            menu.menu.AppendAction("Google Sheets/Selected Table",
                _ => RunExportSelected(useCase, new GoogleSheetsBackend()),
                _ => SelectedStatus(useCase));
        }

        public static void BuildToolbarImportMenu(ToolbarMenu menu, IODDBEditorUseCase useCase)
        {
            menu.menu.AppendAction("CSV/All Tables",
                _ => RunImport(useCase, ExportScope.EntireDatabase, new CsvSheetBackend()));
            menu.menu.AppendAction("CSV/Selected Table",
                _ => RunImportSelected(useCase, new CsvSheetBackend()),
                _ => SelectedStatus(useCase));
            menu.menu.AppendAction("Google Sheets/All Tables",
                _ => RunImport(useCase, ExportScope.EntireDatabase, new GoogleSheetsBackend()));
            menu.menu.AppendAction("Google Sheets/Selected Table",
                _ => RunImportSelected(useCase, new GoogleSheetsBackend()),
                _ => SelectedStatus(useCase));
        }

        public static void AppendTableContextMenu(GenericMenu menu, IODDBEditorUseCase useCase, string tableId)
        {
            if (string.IsNullOrEmpty(tableId) || useCase == null) return;

            var capturedId = tableId;
            var scope = ExportScope.SingleTable(capturedId);

            menu.AddItem(new GUIContent("Export/CSV"), false,
                () => RunExport(useCase, scope, new CsvSheetBackend()));
            menu.AddItem(new GUIContent("Export/Google Sheets"), false,
                () => RunExport(useCase, scope, new GoogleSheetsBackend()));
            menu.AddItem(new GUIContent("Import/CSV"), false,
                () => RunImport(useCase, scope, new CsvSheetBackend()));
            menu.AddItem(new GUIContent("Import/Google Sheets"), false,
                () => RunImport(useCase, scope, new GoogleSheetsBackend()));
        }

        private static DropdownMenuAction.Status SelectedStatus(IODDBEditorUseCase useCase)
        {
            return useCase != null && useCase.TryGetSelectedTableId(out _)
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
        }

        private static void RunExportSelected(IODDBEditorUseCase useCase, ISheetBackend backend)
        {
            if (!useCase.TryGetSelectedTableId(out var tableId))
            {
                EditorUtility.DisplayDialog("ODDB Export", "No table is currently selected.", "OK");
                return;
            }
            RunExport(useCase, ExportScope.SingleTable(tableId), backend);
        }

        private static void RunImportSelected(IODDBEditorUseCase useCase, ISheetBackend backend)
        {
            if (!useCase.TryGetSelectedTableId(out var tableId))
            {
                EditorUtility.DisplayDialog("ODDB Import", "No table is currently selected.", "OK");
                return;
            }
            RunImport(useCase, ExportScope.SingleTable(tableId), backend);
        }

        private static async void RunExport(IODDBEditorUseCase useCase, ExportScope scope, ISheetBackend backend)
        {
            var title = $"ODDB Export ({backend.DisplayName})";
            if (ODDBProgressScope.IsActive)
            {
                EditorUtility.DisplayDialog(title, "Another ODDB import/export operation is already running.", "OK");
                return;
            }

            try
            {
                using (var progress = ODDBProgressScope.Show(title, "Preparing export...", 0f))
                {
                    await useCase.ExportAsync(scope, backend, progress);
                }
                EditorUtility.DisplayDialog(title, $"Export completed ({scope}).", "OK");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog(title, $"Export failed: {e.Message}", "OK");
            }
        }

        private static async void RunImport(IODDBEditorUseCase useCase, ExportScope scope, ISheetBackend backend)
        {
            var title = $"ODDB Import ({backend.DisplayName})";
            if (ODDBProgressScope.IsActive)
            {
                EditorUtility.DisplayDialog(title, "Another ODDB import/export operation is already running.", "OK");
                return;
            }

            var confirm = EditorUtility.DisplayDialog(
                title,
                $"Import will overwrite current data ({scope}).\n" +
                "A pre-import backup will be created automatically.\nContinue?",
                "Import",
                "Cancel");
            if (!confirm) return;

            try
            {
                using (var progress = ODDBProgressScope.Show(title, "Preparing import...", 0f))
                {
                    await useCase.ImportAsync(scope, backend, progress);
                }
                EditorUtility.DisplayDialog(title, $"Import completed ({scope}).", "OK");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog(title, $"Import failed: {e.Message}", "OK");
            }
        }

    }
}
