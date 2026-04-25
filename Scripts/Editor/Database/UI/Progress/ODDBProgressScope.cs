using System;

namespace TeamODD.ODDB.Editors.UI.Progress
{
    internal sealed class ODDBProgressScope : IDisposable, IProgress<float>, IODDBProgressReporter
    {
        private static int _activeCount;

        private readonly string _title;
        private string _message;
        private float _progress;
        private ODDBProgressWindow _window;
        private bool _disposed;

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

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _activeCount = Math.Max(0, _activeCount - 1);
            if (_window != null)
                _window.CompleteAndClose();
        }

        private void HandleWindowClosed(ODDBProgressWindow window)
        {
            if (_window == window)
                _window = null;

            if (_disposed) return;

            UnityEditor.EditorApplication.delayCall += () => RestoreWindow(_progress);
        }
    }
}
