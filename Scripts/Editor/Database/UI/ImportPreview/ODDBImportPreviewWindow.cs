using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Editors.Utils.Sheets.Diff;
using TeamODD.ODDB.Editors.Utils.Sheets.Validation;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI.ImportPreview
{
    internal sealed class ODDBImportPreviewWindow : EditorWindow
    {
        private readonly HashSet<string> _expandedRows = new();
        private SheetImportDiffReport _diffReport;
        private SheetValidationReport _validationReport;
        private Action<bool> _completed;
        private Vector2 _scroll;

        private const float WindowWidth = 620f;
        private const float WindowHeight = 460f;

        public static Task<bool> ShowAsync(
            SheetImportDiffReport diffReport,
            SheetValidationReport validationReport,
            CancellationToken ct)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var window = CreateInstance<ODDBImportPreviewWindow>();
            CancellationTokenRegistration registration = default;
            if (ct.CanBeCanceled)
            {
                registration = ct.Register(() =>
                {
                    window.CloseWithoutResult();
                    completion.TrySetCanceled(ct);
                });
            }

            window._diffReport = diffReport;
            window._validationReport = validationReport;
            window._completed = accepted =>
            {
                registration.Dispose();
                completion.TrySetResult(accepted);
            };
            window.titleContent = new GUIContent("ODDB Import Preview");
            window.minSize = new Vector2(WindowWidth, WindowHeight);
            window.maxSize = new Vector2(900f, 720f);
            window.position = GetCenteredPosition(window.minSize);
            window.ShowUtility();
            window.Focus();
            return completion.Task;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Import Preview", EditorStyles.boldLabel);
            DrawSummary();
            EditorGUILayout.Space(8f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawValidationWarnings();
            DrawSheetDiffs();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(100f)))
                Complete(false);
            if (GUILayout.Button("Apply Import", GUILayout.Width(120f)))
                Complete(true);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void OnDestroy()
        {
            ReleaseGuiFocus();
            var completed = _completed;
            _completed = null;
            completed?.Invoke(false);
        }

        private void Complete(bool accepted)
        {
            var completed = _completed;
            _completed = null;
            ReleaseGuiFocus();
            completed?.Invoke(accepted);
            Close();
            GUIUtility.ExitGUI();
        }

        private void CloseWithoutResult()
        {
            _completed = null;
            ReleaseGuiFocus();
            Close();
        }

        private void DrawSummary()
        {
            if (_diffReport == null)
            {
                EditorGUILayout.HelpBox("No diff data is available.", MessageType.Warning);
                return;
            }

            var summary =
                $"Added: {_diffReport.TotalAddedRows}    " +
                $"Updated: {_diffReport.TotalUpdatedRows}    " +
                $"Removed: {_diffReport.TotalRemovedRows}    " +
                $"Skipped Sheets: {_diffReport.SkippedSheetCount}";
            EditorGUILayout.HelpBox(summary, MessageType.Info);
        }

        private void DrawValidationWarnings()
        {
            if (_validationReport == null || _validationReport.WarningCount == 0)
                return;

            EditorGUILayout.LabelField("Validation Warnings", EditorStyles.boldLabel);
            foreach (var issue in _validationReport.Issues)
            {
                if (issue.Severity == SheetValidationSeverity.Warning)
                    EditorGUILayout.HelpBox(issue.ToString(), MessageType.Warning);
            }
            EditorGUILayout.Space(6f);
        }

        private void DrawSheetDiffs()
        {
            if (_diffReport == null)
                return;

            foreach (var sheet in _diffReport.Sheets)
            {
                EditorGUILayout.LabelField($"{sheet.SheetName} ({sheet.SheetId})", EditorStyles.boldLabel);
                if (sheet.Skipped)
                {
                    EditorGUILayout.HelpBox(sheet.SkipReason, MessageType.Warning);
                    continue;
                }

                EditorGUILayout.LabelField(
                    $"Added {sheet.AddedRows}, Updated {sheet.UpdatedRows}, Removed {sheet.RemovedRows}");

                DrawRows(sheet);
                EditorGUILayout.Space(6f);
            }
        }

        private void DrawRows(SheetImportSheetDiff sheet)
        {
            var rows = sheet.Rows;
            var drawn = 0;
            for (var i = 0; i < rows.Count && drawn < 80; i++)
            {
                if (rows[i].Kind == SheetImportDiffKind.Unchanged)
                    continue;

                DrawRow(sheet, rows[i]);
                drawn++;
            }

            var hiddenChangedRows = CountChangedRows(rows) - drawn;
            if (hiddenChangedRows > 0)
                EditorGUILayout.LabelField($"... {hiddenChangedRows} more changed row(s)");
        }

        private void DrawRow(SheetImportSheetDiff sheet, SheetImportRowDiff row)
        {
            var previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = GetBackgroundColor(row.Kind);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = previousBackground;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(GetKindLabel(row.Kind), GetKindStyle(row.Kind), GUILayout.Width(82f));
            EditorGUILayout.LabelField(row.RowId, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(row.Summary, GUILayout.MaxWidth(260f));
            EditorGUILayout.EndHorizontal();

            if (row.Kind == SheetImportDiffKind.Updated && row.CellChanges.Count > 0)
            {
                var key = $"{sheet.SheetId}:{row.RowId}";
                var expanded = _expandedRows.Contains(key);
                var nextExpanded = EditorGUILayout.Foldout(
                    expanded,
                    $"Changed columns ({row.CellChanges.Count})",
                    true);
                if (nextExpanded)
                    _expandedRows.Add(key);
                else
                    _expandedRows.Remove(key);

                if (nextExpanded)
                    DrawCellChanges(row);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawCellChanges(SheetImportRowDiff row)
        {
            if (row.CellChanges == null || row.CellChanges.Count == 0)
                return;

            EditorGUI.indentLevel++;
            foreach (var change in row.CellChanges)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(change.ColumnName, EditorStyles.boldLabel, GUILayout.Width(150f));
                EditorGUILayout.LabelField(FormatValue(change.OldValue), GUILayout.MinWidth(120f));
                GUILayout.Label("->", GUILayout.Width(24f));
                EditorGUILayout.LabelField(FormatValue(change.NewValue), GUILayout.MinWidth(120f));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private static string GetKindLabel(SheetImportDiffKind kind)
        {
            switch (kind)
            {
                case SheetImportDiffKind.Added:
                    return "ADDED";
                case SheetImportDiffKind.Updated:
                    return "UPDATED";
                case SheetImportDiffKind.Removed:
                    return "REMOVED";
                default:
                    return kind.ToString().ToUpperInvariant();
            }
        }

        private static GUIStyle GetKindStyle(SheetImportDiffKind kind)
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = GetTextColor(kind) }
            };
        }

        private static Color GetTextColor(SheetImportDiffKind kind)
        {
            switch (kind)
            {
                case SheetImportDiffKind.Added:
                    return new Color(0.15f, 0.55f, 0.2f);
                case SheetImportDiffKind.Updated:
                    return new Color(0.75f, 0.45f, 0.05f);
                case SheetImportDiffKind.Removed:
                    return new Color(0.75f, 0.15f, 0.15f);
                default:
                    return EditorStyles.label.normal.textColor;
            }
        }

        private static Color GetBackgroundColor(SheetImportDiffKind kind)
        {
            switch (kind)
            {
                case SheetImportDiffKind.Added:
                    return new Color(0.8f, 1f, 0.82f);
                case SheetImportDiffKind.Updated:
                    return new Color(1f, 0.92f, 0.72f);
                case SheetImportDiffKind.Removed:
                    return new Color(1f, 0.78f, 0.78f);
                default:
                    return GUI.backgroundColor;
            }
        }

        private static int CountChangedRows(IReadOnlyList<SheetImportRowDiff> rows)
        {
            var count = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Kind != SheetImportDiffKind.Unchanged)
                    count++;
            }
            return count;
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrEmpty(value) ? "<empty>" : value;
        }

        private static void ReleaseGuiFocus()
        {
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
            EditorGUIUtility.editingTextField = false;
        }

        private static Rect GetCenteredPosition(Vector2 size)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            return new Rect(
                main.x + (main.width - size.x) * 0.5f,
                main.y + (main.height - size.y) * 0.5f,
                size.x,
                size.y);
        }
    }
}
