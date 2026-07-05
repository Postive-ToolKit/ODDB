using System;
using TeamODD.ODDB.Editors.Window;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI.Dialogs
{
    public sealed class ODDBChangeIdWindow : EditorWindow
    {
        private IODDBEditorUseCase _useCase;
        private string _viewId;
        private string _tableId;
        private string _rowId;
        private bool _rowMode;
        private TextField _idField;
        private Label _messageLabel;

        public static void ShowForView(IODDBEditorUseCase useCase, string viewId)
        {
            if (useCase == null || string.IsNullOrEmpty(viewId))
                return;

            var window = CreateInstance<ODDBChangeIdWindow>();
            window._useCase = useCase;
            window._viewId = viewId;
            window._rowMode = false;
            window.titleContent = new GUIContent("Change ID");
            window.minSize = new Vector2(360, 124);
            window.ShowUtility();
        }

        public static void ShowForRow(IODDBEditorUseCase useCase, string tableId, string rowId)
        {
            if (useCase == null || string.IsNullOrEmpty(tableId) || string.IsNullOrEmpty(rowId))
                return;

            var window = CreateInstance<ODDBChangeIdWindow>();
            window._useCase = useCase;
            window._tableId = tableId;
            window._rowId = rowId;
            window._rowMode = true;
            window.titleContent = new GUIContent("Change ID");
            window.minSize = new Vector2(360, 124);
            window.ShowUtility();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            var currentId = _rowMode ? _rowId : _viewId;
            root.Add(new Label(_rowMode ? $"Row ID: {_rowId}" : $"View/Table ID: {_viewId}"));

            _idField = new TextField("New ID") { value = currentId };
            _idField.RegisterCallback<KeyDownEvent>(OnKeyDown);
            root.Add(_idField);

            _messageLabel = new Label
            {
                style =
                {
                    color = new Color(0.9f, 0.25f, 0.2f),
                    minHeight = 18,
                    whiteSpace = WhiteSpace.Normal,
                }
            };
            root.Add(_messageLabel);

            var buttons = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    marginTop = 4,
                }
            };

            var cancelButton = new Button(Close) { text = "Cancel" };
            var applyButton = new Button(Apply) { text = "Apply" };
            buttons.Add(cancelButton);
            buttons.Add(applyButton);
            root.Add(buttons);

            _idField.Focus();
            _idField.SelectAll();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                Apply();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                evt.StopPropagation();
            }
        }

        private void Apply()
        {
            try
            {
                var newId = _idField.value?.Trim() ?? string.Empty;
                if (_rowMode)
                    _useCase.SetRowId(_tableId, _rowId, newId);
                else
                    _useCase.SetViewId(_viewId, newId);
                Close();
            }
            catch (Exception ex)
            {
                _messageLabel.text = ex.Message;
            }
        }
    }
}
