using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Editors.Utils.Sheets.CSV;
using TeamODD.ODDB.Editors.UI.Progress;
using TeamODD.ODDB.Editors.Settings;
using TeamODD.ODDB.Runtime;
using UnityEditor;

namespace TeamODD.ODDB.Editors.Utils.Sheets.Backends
{
    public sealed class CsvSheetBackend : ISheetBackend
    {
        public const string ParamDirectory = "directory";

        public string DisplayName => "CSV";
        public bool SupportsPartial => true;

        public Task<BackendContext> PrepareAsync(ExportScope scope, BackendIntent intent)
        {
            var title = intent == BackendIntent.Export
                ? "Select folder to save CSV files"
                : "Select folder containing CSV files";

            var directory = EditorUtility.OpenFolderPanel(title, "", "");
            if (string.IsNullOrEmpty(directory))
                return Task.FromResult(BackendContext.Cancel(scope, intent));

            var parameters = new Dictionary<string, object> { { ParamDirectory, directory } };
            return Task.FromResult(BackendContext.Ready(scope, intent, parameters));
        }

        public Task<IReadOnlyList<SheetInfo>> LoadAsync(
            BackendContext ctx,
            IProgress<float> progress,
            CancellationToken ct)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var directory = ctx.GetParameter<string>(ParamDirectory);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                throw new InvalidOperationException(
                    $"CsvSheetBackend.LoadAsync requires '{ParamDirectory}' pointing to an existing directory.");

            // Load every candidate and let the use case unpack grouped files before
            // applying ExportScope. A grouped file is keyed by its root View ID, not
            // by the selected descendant Table ID.
            var files = Directory.GetFiles(directory, "*.csv");
            var result = new List<SheetInfo>(files.Length);
            var database = ODDBEditorRuntime.UseCase?.DataBase as ODDatabase;
            if (files.Length == 0)
            {
                ReportStage(progress, "No matching CSV files found.", 1f);
                return Task.FromResult<IReadOnlyList<SheetInfo>>(result);
            }
            for (var i = 0; i < files.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                var fileName = Path.GetFileName(files[i]);
                var value = (i + 1f) / files.Length;
                ReportStage(progress, $"Reading CSV ({i + 1}/{files.Length}): {fileName}", value);
                if (CSVUtility.TryImportSingleSheet(files[i], out var sheet))
                {
                    CsvSheetViewTypeMetadata.RestoreForImport(sheet, database);
                    result.Add(sheet);
                }
            }
            return Task.FromResult<IReadOnlyList<SheetInfo>>(result);
        }

        public Task SaveAsync(
            BackendContext ctx,
            IReadOnlyList<SheetInfo> sheets,
            IProgress<float> progress,
            CancellationToken ct)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (sheets == null) throw new ArgumentNullException(nameof(sheets));

            var directory = ctx.GetParameter<string>(ParamDirectory);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException(
                    $"CsvSheetBackend.SaveAsync requires '{ParamDirectory}' parameter.");

            Directory.CreateDirectory(directory);

            // The use case already selected the exact physical sheets to export.
            // Re-filtering by TargetTableId would discard a grouped root-View sheet.
            var filtered = FilterSheets(sheets, ExportScope.EntireDatabase);
            var database = ODDBEditorRuntime.UseCase?.DataBase as ODDatabase;
            if (filtered.Count == 0)
            {
                ReportStage(progress, "No sheets to write.", 1f);
                return Task.CompletedTask;
            }
            for (var i = 0; i < filtered.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var value = (i + 1f) / filtered.Count;
                ReportStage(progress, $"Writing CSV ({i + 1}/{filtered.Count}): {filtered[i]?.Name}", value);
                CSVUtility.ExportSingleSheetToCSV(
                    directory,
                    CsvSheetViewTypeMetadata.PrepareForExport(filtered[i], database));
            }
            if (ODDBEditorSettings.Setting.SheetLayoutMode == SheetLayoutMode.GroupByRootView)
                RemoveMovedTablesFromOtherGroups(directory, filtered, database);
            return Task.CompletedTask;
        }

        internal static void RemoveMovedTablesFromOtherGroups(
            string directory,
            IReadOnlyList<SheetInfo> exportedSheets,
            ODDatabase database = null)
        {
            var exportedTableIds = new HashSet<string>(StringComparer.Ordinal);
            var exportedPhysicalIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sheet in exportedSheets)
            {
                if (sheet == null)
                    continue;
                exportedPhysicalIds.Add(sheet.ID);
                if (GroupedSheetCodec.IsGrouped(sheet))
                {
                    foreach (var table in GroupedSheetCodec.Unpack(sheet))
                        exportedTableIds.Add(table.ID);
                }
                else if (!string.IsNullOrEmpty(sheet.ID))
                {
                    exportedTableIds.Add(sheet.ID);
                }
            }

            if (exportedTableIds.Count == 0)
                return;

            foreach (var path in Directory.GetFiles(directory, "*.csv"))
            {
                if (!CSVUtility.TryImportSingleSheet(path, out var existing)
                    || exportedPhysicalIds.Contains(existing.ID))
                {
                    continue;
                }

                CsvSheetViewTypeMetadata.RestoreForImport(existing, database);
                if (!GroupedSheetCodec.IsGrouped(existing))
                    continue;

                var tables = GroupedSheetCodec.Unpack(existing);
                var remaining = tables
                    .Where(table => !exportedTableIds.Contains(table.ID))
                    .ToList();
                if (remaining.Count == tables.Count)
                    continue;

                CSVUtility.ExportSingleSheetToCSV(
                    directory,
                    CsvSheetViewTypeMetadata.PrepareForExport(
                        GroupedSheetCodec.Pack(existing.Name, existing.ID, remaining),
                        database));
            }
        }

        private static void ReportStage(IProgress<float> progress, string stage, float value)
        {
            ODDBProgress.Report(progress, stage, value);
        }

        private static string[] FilterFiles(string[] files, ExportScope scope)
        {
            if (scope.All) return files;

            var matches = new List<string>();
            foreach (var path in files)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var parts = name.Split('_');
                if (parts.Length >= 2 && parts[parts.Length - 1] == scope.TargetTableId)
                    matches.Add(path);
            }
            return matches.ToArray();
        }

        private static List<SheetInfo> FilterSheets(IReadOnlyList<SheetInfo> sheets, ExportScope scope)
        {
            if (scope.All)
            {
                var all = new List<SheetInfo>(sheets.Count);
                foreach (var sheet in sheets)
                    if (sheet != null) all.Add(sheet);
                return all;
            }

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
