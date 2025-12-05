using System.IO;
using TeamODD.ODDB.Editors.UI;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Settings;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace TeamODD.ODDB.Editors.Window
{
    public class ODDBEditorWindow : EditorWindow
    {
        private IODDBEditorUseCase _editorUseCase;
        #region Layout
        private TwoPaneSplitView _splitView;
        private ODDBTreeView _tableTreeView;
        private ODDBHistoryView _historyView;
        private ODDBEditorView _editorView;
        #endregion

        [MenuItem(ODDBEditorConst.MENU_ROOT + "ODDB Editor")]
        public static void OpenWindow()
        {
            ODDBEditorWindow wnd = GetWindow<ODDBEditorWindow>();
            wnd.titleContent = new GUIContent("ODDB Editor");
            wnd.minSize = new Vector2(800, 600);
        }

        public void CreateGUI()
        {
            _editorUseCase = new ODDBEditorUseCase();
            ODDBEditorDI.RegisterSelfAndInterfaces(_editorUseCase);
            ODDBEditorDI.RegisterSelfAndInterfaces(_editorUseCase.DataBase);
            CreateLayout();
            
            _tableTreeView.OnViewSelected += _editorView.SetView;
            
            // bind save key to window not view
            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey)
                {
                    if (evt.keyCode == KeyCode.S)
                    {
                        var fullPath = Path.Combine(ODDBSettings.Setting.Path, ODDBSettings.Setting.DBName);
                        _editorUseCase.SaveDatabase(fullPath);
                    }
                    else if (evt.keyCode == KeyCode.Z)
                    {
                        _editorUseCase.Undo();
                    }
                    else if (evt.keyCode == KeyCode.Y)
                    {
                        _editorUseCase.Redo();
                    }
                }
            });
        }

        private void CreateLayout()
        {
            _splitView = new TwoPaneSplitView
            {
                style = {
                    flexGrow = 1
                },
                fixedPaneIndex = 0,
                fixedPaneInitialDimension = 250
            };
            var treeViewContainer = new VisualElement() { style = { flexGrow = 1 } };
            _tableTreeView = new ODDBTreeView(typeof(View), typeof(Table));
            
            var leftToolbar = new Toolbar();
            var toolBarMenu = new ToolbarMenu();
            toolBarMenu.text = "Table";
            toolBarMenu.menu.AppendAction("Table", action =>
            {
                _tableTreeView.SetTypes(typeof(View), typeof(Table));
                toolBarMenu.text = "Table";
            });
            toolBarMenu.menu.AppendAction("View", action =>
            {
                _tableTreeView.SetTypes(typeof(View));
                toolBarMenu.text = "View";
            });
            leftToolbar.Add(toolBarMenu);

            var toolBarButton = new ToolbarButton();
            toolBarButton.text = "Selected : None";
            
            _tableTreeView.OnViewSelected += view =>
                toolBarButton.text = "Selected : " + _editorUseCase.GetViewName(view);
            
            leftToolbar.Add(toolBarButton);

            var historyToggle = new ToolbarToggle { text = "History" };
            historyToggle.RegisterValueChangedCallback(evt =>
            {
                _historyView.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            leftToolbar.Add(historyToggle);
            
            treeViewContainer.Add(leftToolbar);
            treeViewContainer.Add(_tableTreeView);
            
            _historyView = new ODDBHistoryView { style = { display = DisplayStyle.None, height = 150 } };
            treeViewContainer.Add(_historyView);

            _splitView.Add(treeViewContainer);
            
            _editorView = new ODDBEditorView();
            _splitView.Add(_editorView);
            
            rootVisualElement.Add(_splitView);
        }

        private void OnDestroy()
        {
            var result = EditorUtility.DisplayDialog("Save Changes", "Do you want to save changes?", "Yes", "No");
            if (result)
            {
                var fullPath = Path.Combine(ODDBSettings.Setting.Path, ODDBSettings.Setting.DBName);
                _editorUseCase.SaveDatabase(fullPath);
            }
            _editorUseCase?.Dispose();
            ODDBEditorDI.DisposeAll();
        }
    }
}
