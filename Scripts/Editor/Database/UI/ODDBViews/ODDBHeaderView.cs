using System;
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
        private ODDBViewType _type;
        public Action<ODDBViewType> OnTypeChanged;

        public ODDBHeaderView(IODDBEditorUseCase editorUseCase)
        {
            _editorUseCase = editorUseCase;
            _toolbar = new Toolbar { style = { flexShrink = 1 } };
            Add(_toolbar);
        }

        public void UpdateView(IView view, ODDBViewType type)
        {
            _view = view;
            _type = type;
            Rebuild();
        }

        public void ClearView()
        {
            _view = null;
            _toolbar.Clear();
        }

        private void Rebuild()
        {
            _toolbar.Clear();
            if (_view == null) return;

            // Name
            var nameButton = new ToolbarButton { text = "Name" };
            _toolbar.Add(nameButton);
            var nameTextField = new TextField { value = _view.Name, style = { minWidth = 200 } };
            nameTextField.RegisterValueChangedCallback(evt =>
            {
                _editorUseCase.SetViewName(_view.ID, evt.newValue);
            });
            _toolbar.Add(nameTextField);

            // ID
            var idButton = new ToolbarButton { text = "ID" };
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
            idTextField.SetEnabled(false);
            _toolbar.Add(idTextField);

            // Type Menu
            var editorMenu = new ToolbarMenu { text = _type.ToString() };
            editorMenu.menu.AppendAction("View", _ => OnTypeChanged?.Invoke(ODDBViewType.View));
            
            if (_type == ODDBViewType.Table)
            {
                editorMenu.menu.AppendAction("Table", _ => OnTypeChanged?.Invoke(ODDBViewType.Table));
            }
            _toolbar.Add(editorMenu);
        }
    }
}