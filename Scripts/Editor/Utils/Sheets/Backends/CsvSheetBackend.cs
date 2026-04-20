using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Plugins.ODDB.Scripts.Editor.Utils.Sheets.CSV;
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

            var files = FilterFiles(Directory.GetFiles(directory, "*.csv"), ctx.Scope);
            var result = new List<SheetInfo>(files.Length);
            for (var i = 0; i < files.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (CSVUtility.TryImportSingleSheet(files[i], out var sheet))
                    result.Add(sheet);
                progress?.Report(files.Length == 0 ? 1f : (i + 1f) / files.Length);
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

            var filtered = FilterSheets(sheets, ctx.Scope);
            for (var i = 0; i < filtered.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                CSVUtility.ExportSingleSheetToCSV(directory, filtered[i]);
                progress?.Report(filtered.Count == 0 ? 1f : (i + 1f) / filtered.Count);
            }
            return Task.CompletedTask;
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
