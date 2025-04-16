using System;
using System.IO;
using Plugins.ODDB.Scripts.Runtime.Data.Enum;
using TeamODD.ODDB.Editors.UI;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Utils.Services;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Settings.Data;
using TeamODD.ODDB.Scripts.Runtime.Data;
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
            var visualTree = Resources.Load<VisualTreeAsset>("Uxml_ODDB_Window");
            // Instantiate UXML
            var root = rootVisualElement;
            visualTree.CloneTree(root);

            _dataService = new ODDBDataService();

            _settings = Resources.Load<ODDBSettings>("ODDBSettings");
            if(!_settings.IsInitialized) 
            {
                var pathSelector = new ODDBPathUtility();
                _settings.Path = pathSelector.GetPath(ODDBSettings.BASE_PATH,ODDBSettings.BASE_PATH);
            }
            string fullPath = Path.Combine(_settings.Path, _settings.DBName);
            
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
            var tableDataView = root.Q<ODDBTableDataView>("table-data-view");
            var tableListView = root.Q<ODDBTableListView>("table-list-view");
            tableListView.SetDatabase(_database);
            tableListView.OnTableSelected += tableDataView.SetTable;
            tableDataView.OnTableNameChanged += (table) => {
                tableListView.UpdateTable(table);
            };
            
            // bind save key to window not view
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.S && evt.ctrlKey)
                {
                    _dataService.SaveDatabase(_database, fullPath);
                }
            });
        }
    }
}
