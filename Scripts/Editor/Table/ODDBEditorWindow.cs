using System.IO;
using TeamODD.ODDB.Editors.UI;
using TeamODD.ODDB.Editors.Utils.Services;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Settings.Data;
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

        #region Layout
        
        private ODDBSplitView _splitView;
        private ODDatabaseListView tableListView;
        private ODDBEditorView editorView;
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
            CreateLayout();
            
            tableListView.SetDatabase(_database);
            tableListView.OnViewSelected += editorView.SetView;
            editorView.OnViewDataChanged += (view) => {
                tableListView.SetView(view);
            };
            
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
            
            tableListView = new ODDatabaseListView();
            _splitView.Add(tableListView);
            
            editorView = new ODDBEditorView();
            _splitView.Add(editorView);
            
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
    }
}
