using System.IO;
using TeamODD.ODDB.Editors.UI;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace TeamODD.ODDB.Editors.Window
{
    public class ODDBEditorWindow : EditorWindow
    {
        private ODDatabase _database;
        private ODDBDataService _dataService;
        private ODDBSettings _settings;
        private IODDBEditorUseCase _editorUseCase;
        #region Layout
        private ODDBSplitView _splitView;
        private ODDatabaseTreeView _tableTreeView;
        private ODDBEditorView _editorView;
        #endregion

        [MenuItem("Window/ODDB Editor")]
        public static void ShowExample()
        {
            var settingFiles = Resources.Load<ODDBSettings>("ODDBSettings");
            if (settingFiles == null)
            {
                settingFiles = CreateInstance<ODDBSettings>();
                settingFiles.name = "ODDBSettings";
                // find resources folder
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateAsset(settingFiles, "Assets/Resources/ODDBSettings.asset");
                AssetDatabase.SaveAssets();
            }
            ODDBEditorWindow wnd = GetWindow<ODDBEditorWindow>();
            wnd.titleContent = new GUIContent("ODDB Editor");
            wnd.minSize = new Vector2(800, 600);
        }

        public void CreateGUI()
        {
            Initialize();
            ODDBEditorDI.Register(_database);
            ODDBEditorDI.RegisterSelfAndInterfaces(new ODDBEditorUseCase(_database));
            CreateLayout();
            
            _tableTreeView.OnViewSelected += _editorView.SetView;
            
            // bind save key to window not view
            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.S && evt.ctrlKey)
                {
                    var fullPath = Path.Combine(_settings.Path, _settings.DBName);
                    _dataService.SaveDatabase(_database, fullPath);
                }
            });
        }

        private void CreateLayout()
        {
            _splitView = new ODDBSplitView
            {
                style = {
                    flexGrow = 1
                },
                fixedPaneIndex = 0,
                fixedPaneInitialDimension = 200
            };
            _tableTreeView = new ODDatabaseTreeView();
            var treeViewContainer = new VisualElement() { style = { flexGrow = 1 } };
            treeViewContainer.Add(_tableTreeView);
            _splitView.Add(treeViewContainer);
            
            _editorView = new ODDBEditorView();
            _splitView.Add(_editorView);
            
            rootVisualElement.Add(_splitView);
        }

        private void Initialize()
        {
            _dataService = new ODDBDataService();

            _settings = Resources.Load<ODDBSettings>("ODDBSettings");
            if(!_settings.IsInitialized) 
            {
                var pathSelector = new ODDBPathUtility();
                _settings.Path = pathSelector.GetPath(ODDBSettings.BASE_PATH,ODDBSettings.BASE_PATH);
            }
            var fullPath = Path.Combine(_settings.Path, _settings.DBName);
            
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
                var fullPath = Path.Combine(_settings.Path, _settings.DBName);
                _dataService.SaveDatabase(_database, fullPath);
            }
            _editorUseCase?.Dispose();
            ODDBEditorDI.DisposeAll();
        }
    }
}
