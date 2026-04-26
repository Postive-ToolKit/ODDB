using System;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Editors.UI.Dialogs;
using TeamODD.ODDB.Editors.UI.ImportPreview;
using TeamODD.ODDB.Editors.Utils.Sheets.Diff;
using TeamODD.ODDB.Editors.Utils.Sheets.Validation;

namespace TeamODD.ODDB.Editors.UI.Progress
{
    internal sealed class ODDBProgressScope : IDisposable, IProgress<float>, IODDBProgressReporter, IODDBImportPreviewPresenter
    {
        private static int _activeCount;

        private readonly string _title;
        private string _message;
        private float _progress;
        private ODDBProgressWindow _window;
        private bool _disposed;
        private bool _closingIntentionally;

        private ODDBProgressScope(string title, string message, float progress)
        {
            _activeCount++;
            _title = string.IsNullOrEmpty(title) ? "ODDB" : title;
            _message = string.IsNullOrEmpty(message) ? "Working..." : message;
            _progress = progress;
            RestoreWindow(progress);
        }

        public static bool IsActive => _activeCount > 0;

        public static ODDBProgressScope Show(string title, string message, float progress)
        {
            return new ODDBProgressScope(title, message, progress);
        }

        public void Report(float value)
        {
            Report(_message, value);
        }

        public void Report(string message, float value)
        {
            if (_disposed) return;

            _message = string.IsNullOrEmpty(message) ? _message : message;
            _progress = value;
            if (_window == null)
                RestoreWindow(value);

            _window.UpdateProgress(_message, value);
        }

        public void RestoreWindow(float progress)
        {
            if (_disposed || _window != null) return;

            _window = ODDBProgressWindow.ShowFocused(_title, _message, progress, HandleWindowClosed);
        }

        public Task<bool> ShowImportPreviewAsync(
            SheetImportDiffReport diffReport,
            SheetValidationReport validationReport,
            CancellationToken ct)
        {
            if (_disposed)
                return Task.FromResult(false);

            CloseCurrentWindow();
            return ShowImportPreviewAndRestoreAsync(diffReport, validationReport, ct);
        }

        public Task ShowResultAsync(string message, bool isError)
        {
            if (_disposed)
                return Task.CompletedTask;

            CloseCurrentWindow();
            return ODDBResultWindow.ShowAsync(_title, message, isError);
        }

        private async Task<bool> ShowImportPreviewAndRestoreAsync(
            SheetImportDiffReport diffReport,
            SheetValidationReport validationReport,
            CancellationToken ct)
        {
            var accepted = await ODDBImportPreviewWindow.ShowAsync(diffReport, validationReport, ct);
            if (accepted && !_disposed)
            {
                _message = "Applying import...";
                _progress = Math.Max(_progress, 0.95f);
                RestoreWindow(_progress);
                _window?.UpdateProgress(_message, _progress);
            }

            return accepted;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _activeCount = Math.Max(0, _activeCount - 1);
            if (_window != null)
                _window.CompleteAndClose();
        }

        private void CloseCurrentWindow()
        {
            if (_window == null)
                return;

            _closingIntentionally = true;
            _window.CompleteAndClose();
        }

        private void HandleWindowClosed(ODDBProgressWindow window)
        {
            if (_window == window)
                _window = null;

            if (_disposed) return;
            if (_closingIntentionally)
            {
                _closingIntentionally = false;
                return;
            }

            UnityEditor.EditorApplication.delayCall += () => RestoreWindow(_progress);
        }
    }
}
