using System;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI.ViewWindows
{
    public class ODDBViewSelectorWindow: EditorWindow
    {
        private string _inputValue = "";
        public event Action<string> OnConfirm;
        public event Action OnCancel; 
        private void OnGUI()
        {
            GUILayout.Space(10);
            GUI.SetNextControlName("InputField");
            // match the input field with the window title align center
            _inputValue = EditorGUILayout.TextField(_inputValue);
            
            // 자동으로 입력 필드에 포커스
            if (Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("InputField");
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var isEnterPressed = (Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.KeyDown);
            if ((GUILayout.Button("Confirm", GUILayout.Width(60)) || isEnterPressed) && !string.IsNullOrEmpty(_inputValue))
            {
                ConfirmInput();
                return;
            }
            
            if (GUILayout.Button("Cancel", GUILayout.Width(60)))
            {
                Close();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            //Align the center

            if (string.IsNullOrEmpty(_inputValue))
            {
                // if input value is empty show red color text that saying "Input is required"
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.red;
                style.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("Input is required", style);
            }

        }

        private void ConfirmInput()
        {
            if (!string.IsNullOrEmpty(_inputValue))
            {
                OnConfirm?.Invoke(_inputValue);
                Close();
            }
        }
        public class Builder
        {
            private string _title;
            private Action<string> _onConfirm;
            private Action _onCancel;
            public Builder SetTitle(string title)
            {
                _title = title;
                return this;
            }
            public Builder SetOnConfirm(Action<string> onConfirm)
            {
                _onConfirm = onConfirm;
                return this;
            }
            public Builder SetOnCancel(Action onCancel)
            {
                _onCancel = onCancel;
                return this;
            }
            public ODDBStringInputWindow Build()
            {
                var window = GetWindow<ODDBStringInputWindow>(true);
                window.titleContent = new GUIContent(_title);
                window.OnConfirm += _onConfirm;
                window.OnCancel += _onCancel;
                window.position = new Rect(Screen.currentResolution.width/2 - 150, Screen.currentResolution.height/2 - 50, 300, 100);
                return window;
            }
        }
    }
}