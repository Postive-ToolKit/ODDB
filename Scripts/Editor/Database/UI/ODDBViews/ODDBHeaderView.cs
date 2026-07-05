using System;
using TeamODD.ODDB.Editors.UI.Dialogs;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDBHeaderView : VisualElement
    {
        private readonly IODDBEditorUseCase _editorUseCase;
        private readonly Toolbar _toolbar;
        private IView _view;
        private ODDBViewType _mode;
        public Action<ODDBViewType> OnTypeChanged;

        public ODDBHeaderView(IODDBEditorUseCase editorUseCase)
        {
            _editorUseCase = editorUseCase;
            _toolbar = new Toolbar { style = { flexShrink = 1 } };
            Add(_toolbar);
        }

        public void UpdateView(IView view, ODDBViewType mode)
        {
            _view = view;
            _mode = mode;
            Rebuild();
        }

        public void ClearView()
        {
            _view = null;
            _mode = ODDBViewType.None;
            _toolbar.Clear();
        }

        private void Rebuild()
        {
            _toolbar.Clear();
            if (_view == null) return;

            var selectedType = _view is Table ? ODDBViewType.Table : ODDBViewType.View;

            var titleLabel = new Label($"Selected {selectedType}") 
            { 
                style = 
                { 
                    unityFontStyleAndWeight = FontStyle.Bold, 
                    unityTextAlign = TextAnchor.MiddleLeft, 
                    paddingLeft = 5, 
                    paddingRight = 5 
                } 
            };
            _toolbar.Add(titleLabel);

            // Name
            var nameButton = new ToolbarButton { text = "Name" };
            nameButton.tooltip = "The name of this View/Table";
            _toolbar.Add(nameButton);
            var nameTextField = new TextField { value = _view.Name, style = { minWidth = 200 } };
            nameTextField.tooltip = "Edit the display name used inside the ODDB editor";
            nameTextField.RegisterValueChangedCallback(evt =>
            {
                _editorUseCase.SetViewName(_view.ID, evt.newValue);
            });
            _toolbar.Add(nameTextField);

            // ID
            var idButton = new ToolbarButton { text = "ID" };
            idButton.tooltip = "Click to copy ID to clipboard";
            idButton.RegisterCallback<ClickEvent>(evt =>
            {
                EditorGUIUtility.systemCopyBuffer = _view.ID;
                Debug.Log($"Copied ID : {_view.ID} to Clipboard");
            });
            _toolbar.Add(idButton);
            
            var idTextField = new TextField
            {
                value = _view.ID,
                isReadOnly = true,
                style = { flexGrow = 0, flexShrink = 1 }
            };
            idTextField.tooltip = "Right-click to change ID";
            idTextField.RegisterCallback<ContextClickEvent>(evt =>
            {
                var capturedId = _view?.ID.ToString();
                if (string.IsNullOrEmpty(capturedId))
                    return;

                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Change ID..."), false,
                    () => ODDBChangeIdWindow.ShowForView(_editorUseCase, capturedId));
                menu.ShowAsContext();
                evt.StopPropagation();
            });
            _toolbar.Add(idTextField);

            // Type Menu
            var editorMenu = new ToolbarMenu { text = $"Mode: {GetModeLabel(_mode)}" };
            editorMenu.tooltip = "Switch between View (schema) and Table (data) editing modes";
            editorMenu.menu.AppendAction("View Fields", _ => OnTypeChanged?.Invoke(ODDBViewType.View));
            
            if (selectedType == ODDBViewType.Table)
            {
                editorMenu.menu.AppendAction("Table Rows", _ => OnTypeChanged?.Invoke(ODDBViewType.Table));
            }
            _toolbar.Add(editorMenu);
        }

        private static string GetModeLabel(ODDBViewType mode)
        {
            return mode switch
            {
                ODDBViewType.View => "View Fields",
                ODDBViewType.Table => "Table Rows",
                _ => "None"
            };
        }
    }
}
