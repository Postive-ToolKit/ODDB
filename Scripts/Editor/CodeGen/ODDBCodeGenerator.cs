using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeamODD.ODDB.Editors.Settings;
using TeamODD.ODDB.Editors.UI.Dialogs;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Settings;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace TeamODD.ODDB.Editors.CodeGen
{
    /// <summary>
    /// Entry point that orchestrates the full generation pipeline:
    /// pre-flight → validation → generation → stale cleanup → pending remap → refresh.
    /// </summary>
    internal static class ODDBCodeGenerator
    {
        /// <summary>Generate code for every View/Table in the database.</summary>
        public static void GenerateAll()
        {
            Run(targetViewIds: null);
        }

        /// <summary>Generate code only for the views with the given IDs (no stale cleanup).</summary>
        public static void GenerateSelection(IEnumerable<string> viewIds)
        {
            var ids = viewIds?.Where(id => !string.IsNullOrEmpty(id)).ToList();
            if (ids == null || ids.Count == 0)
            {
                _ = ODDBResultWindow.ShowAsync("ODDB CodeGen", "No views were selected.", isError: true);
                return;
            }
            Run(ids);
        }

        private static void Run(IReadOnlyCollection<string> targetViewIds)
        {
            // 1. Pre-flight
            if (!OutputPathResolver.TryGetValidOutputFolder(out var outputFolder, out var folderReason))
            {
                FocusSettingsAsset();
                _ = ODDBResultWindow.ShowAsync("ODDB CodeGen", folderReason, isError: true);
                return;
            }

            // 2. Load DB
            var dataService = new ODDBDataService();
            var dbPath = Path.Combine(ODDBRuntimeSettings.Setting.Path, ODDBRuntimeSettings.Setting.DBName);
            if (!dataService.LoadDatabase(dbPath, out var database) || database == null)
            {
                _ = ODDBResultWindow.ShowAsync("ODDB CodeGen", $"Failed to load database at {dbPath}", isError: true);
                return;
            }

            // 3. Collect target views
            var allViews = database.GetAll().ToList();
            List<IView> targets = targetViewIds == null
                ? allViews
                : allViews.Where(v => targetViewIds.Contains(v.ID.ToString())).ToList();
            if (targets.Count == 0)
            {
                _ = ODDBResultWindow.ShowAsync("ODDB CodeGen", "No views to generate.", isError: true);
                return;
            }

            // 4. Build batch ClassName map (ViewID → ClassName) — uses View.Name as-is (validation will catch invalid).
            var batchClassNames = targets.ToDictionary(v => v.ID.ToString(), v => v.Name ?? string.Empty);

            // 5. Validate
            var lookup = new DatabaseLookup(allViews);
            var typeMapper = new TypeMapper(batchClassNames);
            var errors = Validate(targets, batchClassNames, typeMapper, lookup, outputFolder);
            if (errors.Count > 0)
            {
                var message = "Generation aborted. Fix the following and re-run:\n\n"
                              + string.Join("\n", errors.Select(e => e.ToDisplayLine()));
                _ = ODDBResultWindow.ShowAsync("ODDB CodeGen — Validation Failed", message, isError: true);
                return;
            }

            // 6. Generate
            var writer = new ViewClassWriter(typeMapper, batchClassNames, lookup);
            var written = new List<string>(targets.Count);
            var pendingPairs = new List<(string viewId, string className)>(targets.Count);
            try
            {
                EditorUtility.DisplayProgressBar("ODDB CodeGen", "Writing classes...", 0f);
                for (int i = 0; i < targets.Count; i++)
                {
                    var view = targets[i];
                    var className = batchClassNames[view.ID.ToString()];
                    var source = writer.Write(view, className);
                    var filePath = Path.Combine(outputFolder, className + ".cs");
                    File.WriteAllText(filePath, source);
                    written.Add(filePath);
                    pendingPairs.Add((view.ID.ToString(), className));
                    EditorUtility.DisplayProgressBar("ODDB CodeGen", $"Writing {className}.cs",
                        (i + 1f) / targets.Count);
                }
                PendingRemapStore.UpsertMany(pendingPairs);

                // 7. Stale cleanup — ALL mode only
                if (targetViewIds == null)
                {
                    var keep = new HashSet<string>(batchClassNames.Values);
                    foreach (var stale in OutputPathResolver.FindStaleFiles(outputFolder, keep).ToList())
                        OutputPathResolver.DeleteFileWithMeta(stale);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // 8. Refresh — Unity compiles, then PendingBindAssigner runs on reload.
            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
            _ = ODDBResultWindow.ShowAsync(
                "ODDB CodeGen",
                $"{written.Count} class(es) written to {OutputPathResolver.ToAssetsRelative(outputFolder)}.\n" +
                "BindType will be assigned automatically after the compile finishes.",
                isError: false);
        }

        private static List<ValidationError> Validate(
            IReadOnlyList<IView> targets,
            Dictionary<string, string> batchClassNames,
            TypeMapper typeMapper,
            IODatabaseView lookup,
            string outputFolder)
        {
            var errors = new List<ValidationError>();
            var seenClassNames = new HashSet<string>();

            foreach (var view in targets)
            {
                var className = batchClassNames[view.ID.ToString()];
                var viewLabel = view.Name ?? "(unnamed)";

                // Class name rules
                if (!ClassNameValidator.IsValidIdentifier(className, out var reason))
                    errors.Add(ValidationError.ForView(viewLabel, $"class name {reason}"));
                else if (!seenClassNames.Add(className))
                    errors.Add(ValidationError.ForView(viewLabel,
                        $"duplicate class name '{className}' in this generation batch"));
                else
                {
                    // Conflict with user-authored .cs in the output folder
                    var candidate = Path.Combine(outputFolder, className + ".cs");
                    if (File.Exists(candidate) && !GeneratedFileMarker.IsGenerated(candidate))
                        errors.Add(ValidationError.ForView(viewLabel,
                            $"a non-generated file already exists at {className}.cs — rename it or pick another View name"));
                }

                // Field rules
                var seenFieldNames = new HashSet<string>();
                var inheritedNames = new HashSet<string>(
                    (view.ParentView?.TotalFields ?? new List<Field>()).Select(f => f.Name ?? string.Empty));

                foreach (var field in view.ScopedFields)
                {
                    var fname = field.Name ?? string.Empty;
                    if (!ClassNameValidator.IsValidIdentifier(fname, out var freason))
                        errors.Add(ValidationError.ForField(viewLabel, fname, freason));
                    else
                    {
                        if (!seenFieldNames.Add(fname))
                            errors.Add(ValidationError.ForField(viewLabel, fname, "duplicate field name in this view"));
                        if (inheritedNames.Contains(fname))
                            errors.Add(ValidationError.ForField(viewLabel, fname,
                                "name collides with an inherited field — rename to avoid C# field conflict"));
                    }

                    // Type resolution
                    var resolved = typeMapper.Resolve(field.Type, lookup);
                    if (!resolved.Ok)
                        errors.Add(ValidationError.ForField(viewLabel, fname, resolved.FailureReason));
                }
            }
            return errors;
        }

        private static void FocusSettingsAsset()
        {
            var settings = ODDBEditorSettings.Setting;
            if (settings != null)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
        }

        private sealed class DatabaseLookup : IODatabaseView
        {
            private readonly Dictionary<string, IView> _byId;
            public DatabaseLookup(IEnumerable<IView> views)
            {
                _byId = views.ToDictionary(v => v.ID.ToString(), v => v);
            }
            public IView Find(string viewId)
            {
                return string.IsNullOrEmpty(viewId) ? null : _byId.GetValueOrDefault(viewId);
            }
        }
    }
}
