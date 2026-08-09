using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI.Dialogs
{
    internal sealed class ODDBResultWindow : EditorWindow
    {
        private Action _completed;
        private string _message;
        private bool _isError;
        private Vector2 _scroll;
        private Vector2 _windowSize;

        private const float WindowWidth = 440f;
        private const float WindowHeight = 150f;
        private const float DetailedWindowWidth = 620f;
        private const float DetailedWindowHeight = 420f;

        public static Task ShowAsync(string title, string message, bool isError)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var window = CreateInstance<ODDBResultWindow>();
            window._message = string.IsNullOrEmpty(message) ? "Done." : message;
            window._isError = isError;
            window._windowSize = NeedsDetailedLayout(window._message, isError)
                ? new Vector2(DetailedWindowWidth, DetailedWindowHeight)
                : new Vector2(WindowWidth, WindowHeight);
            window._completed = () => completion.TrySetResult(true);
            window.titleContent = new GUIContent(string.IsNullOrEmpty(title) ? "ODDB" : title);
            window.minSize = window._windowSize;
            window.maxSize = window._windowSize;
            window.CenterOnMainWindow();
            window.ShowUtility();
            window.Focus();
            return completion.Task;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(_isError ? "Operation Failed" : "Operation Completed", EditorStyles.boldLabel);
            EditorGUILayout.Space(10f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.HelpBox(_message ?? "Done.", _isError ? MessageType.Error : MessageType.Info);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK", GUILayout.Width(100f)))
                Complete();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void OnDestroy()
        {
            ReleaseGuiFocus();
            var completed = _completed;
            _completed = null;
            completed?.Invoke();
        }

        private void Complete()
        {
            var completed = _completed;
            _completed = null;
            ReleaseGuiFocus();
            completed?.Invoke();
            Close();
            GUIUtility.ExitGUI();
        }

        private void CenterOnMainWindow()
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            position = new Rect(
                main.x + (main.width - _windowSize.x) * 0.5f,
                main.y + (main.height - _windowSize.y) * 0.5f,
                _windowSize.x,
                _windowSize.y);
        }

        private static bool NeedsDetailedLayout(string message, bool isError)
        {
            return isError
                   && !string.IsNullOrEmpty(message)
                   && (message.Length > 200
                       || message.IndexOf('\n') >= 0
                       || message.IndexOf('\r') >= 0);
        }

        private static void ReleaseGuiFocus()
        {
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
            EditorGUIUtility.editingTextField = false;
        }
    }
}
