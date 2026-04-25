using System;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI.Progress
{
    internal sealed class ODDBProgressWindow : EditorWindow
    {
        private Action<ODDBProgressWindow> _closed;
        private string _message;
        private float _progress;
        private bool _canClose;

        private const float WindowWidth = 440f;
        private const float WindowHeight = 150f;

        public static ODDBProgressWindow ShowFocused(
            string title,
            string message,
            float progress,
            Action<ODDBProgressWindow> closed)
        {
            var window = CreateInstance<ODDBProgressWindow>();
            window._closed = closed;
            window.titleContent = new GUIContent(title);
            window.minSize = new Vector2(WindowWidth, WindowHeight);
            window.maxSize = new Vector2(WindowWidth, WindowHeight);
            window.UpdateProgress(message, progress);
            window.CenterOnMainWindow();
            window.ShowUtility();
            window.Focus();
            EditorApplication.update += window.KeepFocused;
            return window;
        }

        public void UpdateProgress(string message, float progress)
        {
            _message = string.IsNullOrEmpty(message) ? "Working..." : message;
            _progress = Mathf.Clamp01(progress);
            Focus();
            Repaint();
        }

        public void CompleteAndClose()
        {
            _canClose = true;
            Close();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Synchronizing data...", EditorStyles.boldLabel);
            EditorGUILayout.Space(10f);

            var rect = GUILayoutUtility.GetRect(1f, 20f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, _progress, $"{Mathf.RoundToInt(_progress * 100f)}%");

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("Current step", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(_message ?? "Working...", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8f);
        }

        private void CenterOnMainWindow()
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            position = new Rect(
                main.x + (main.width - WindowWidth) * 0.5f,
                main.y + (main.height - WindowHeight) * 0.5f,
                WindowWidth,
                WindowHeight);
        }

        private void OnLostFocus()
        {
            if (_canClose) return;
            Focus();
        }

        private void KeepFocused()
        {
            if (_canClose) return;
            Focus();
        }

        private void OnDestroy()
        {
            EditorApplication.update -= KeepFocused;
            var closed = _closed;
            _closed = null;
            closed?.Invoke(this);
        }
    }
}
