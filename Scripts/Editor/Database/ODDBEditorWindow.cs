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
        private ODDatabase _database;
        private ODDBDataService _dataService;
        private IODDBEditorUseCase _editorUseCase;
        #region Layout
        private TwoPaneSplitView _splitView;
        private ODDBTreeView _tableTreeView;
        private ODDBEditorView _editorView;
        #endregion

        [MenuItem("Window/ODDB Editor")]
        public static void OpenWindow()
        {
            ODDBEditorWindow wnd = GetWindow<ODDBEditorWindow>();
            wnd.titleContent = new GUIContent("ODDB Editor");
            wnd.minSize = new Vector2(800, 600);
        }

        public void CreateGUI()
        {
            Initialize();
            ODDBEditorDI.Register(_database);
            ODDBEditorDI.RegisterSelfAndInterfaces(new ODDBEditorUseCase(_database));
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            CreateLayout();
            
            _tableTreeView.OnViewSelected += _editorView.SetView;
            
            // bind save key to window not view
            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.S && evt.ctrlKey)
                {
                    var fullPath = Path.Combine(ODDBSettings.Setting.Path, ODDBSettings.Setting.DBName);
                    _dataService.SaveDatabase(_database, fullPath);
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
            
            treeViewContainer.Add(leftToolbar);
            treeViewContainer.Add(_tableTreeView);
            _splitView.Add(treeViewContainer);
            
            _editorView = new ODDBEditorView();
            _splitView.Add(_editorView);
            
            rootVisualElement.Add(_splitView);
        }

        private void Initialize()
        {
            _dataService = new ODDBDataService();
            
            if(!ODDBSettings.Setting.IsInitialized) 
            {
                var pathSelector = new ODDBPathUtility();
                ODDBSettings.Setting.Path = pathSelector.GetPath(ODDBSettings.BASE_PATH,ODDBSettings.BASE_PATH);
            }
            
            var fullPath = Path.Combine(ODDBSettings.Setting.Path, ODDBSettings.Setting.DBName);
            
            if (!File.Exists(fullPath))
            {
                Debug.Log($"Creating new database file: {fullPath}");
                _database = new ODDatabase();
                if (_dataService.SaveDatabase(_database, fullPath))
                {
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                if (!_dataService.LoadDatabase(fullPath, out _database))
                {
                    Debug.LogError("Failed to load database");
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            var result = EditorUtility.DisplayDialog("Save Changes", "Do you want to save changes?", "Yes", "No");
            if (result)
            {
                var fullPath = Path.Combine(ODDBSettings.Setting.Path, ODDBSettings.Setting.DBName);
                _dataService.SaveDatabase(_database, fullPath);
            }
            _editorUseCase?.Dispose();
            ODDBEditorDI.DisposeAll();
        }
    }
}
