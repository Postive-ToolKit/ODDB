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

            var settingFiles = Resources.Load<ODDBSettings>("ODDBSettings");
            if(!settingFiles.IsInitialized) 
            {
                var pathSelector = new ODDBPathUtility();
                settingFiles.Path = pathSelector.GetPath(ODDBSettings.BASE_PATH,ODDBSettings.BASE_PATH);
            }
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(settingFiles);

            if (string.IsNullOrEmpty(assetPath))
                return;
            assetPath = assetPath.Replace("Assets/Resources/", "");
            //remove file extension
            assetPath = Path.ChangeExtension(assetPath, null);
            Debug.Log(assetPath);
            
            string fullPath = Path.Combine(settingFiles.Path, settingFiles.DBName);
            
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

            

            root.Q<Button>("button-save")
                .RegisterCallback<ClickEvent>(e =>
                {
                    if (_dataService.SaveDatabase(_database, fullPath))
                    {
                        //AssetDatabase.Refresh();
                    }
                });

            root.Q<Button>("button-load")
                .RegisterCallback<ClickEvent>(e =>
                {
                    var path = settingFiles.Path;
                    var testDataBase = CreateTestDatabase();
                    _dataService.SaveDatabase(testDataBase, path + "/test.db");
                });
            var tableDataView = root.Q<ODDBTableDataView>("table-data-view");
            var tableListView = root.Q<ODDBTableListView>("table-list-view");
            tableListView.SetDatabase(_database);
            tableListView.OnTableSelected += tableDataView.SetTable;
            tableListView.OnTableSelected += OnTableSelected;


            
        }

        private void OnTableSelected(ODDBTable table)
        {
            Debug.Log($"Table selected: {table.Name}");
        }

        private ODDatabase CreateTestDatabase()
        {
            var testDataBase = new ODDatabase();
            var testTable = new ODDBTable();
            testTable.Name = "TestTable";
            testTable.Key = "TestKey";
            testTable.AddField(new ODDBTableMeta() { Name = "TestMeta1", DataType = ODDBDataType.String });
            testTable.AddField(new ODDBTableMeta() { Name = "TestMet3", DataType = ODDBDataType.Int });
            testTable.AddField(new ODDBTableMeta() { Name = "TestMet6", DataType = ODDBDataType.Float });
            testTable.AddField(new ODDBTableMeta() { Name = "TestMe2", DataType = ODDBDataType.Sprite });
            testTable.AddField(new ODDBTableMeta() { Name = "TestBool", DataType = ODDBDataType.Bool });
            testTable.AddField(new ODDBTableMeta() { Name = "Prefab", DataType = ODDBDataType.Prefab });
            
            testTable.AddRow(new ODDBRow(new string[] { "Test1", "1", "1.1", null, "true", null }));
            testTable.AddRow(new ODDBRow(new string[] { "Test3", "2", "2.34" ,null, "false", "SV/GameObject"}));
            
            testDataBase.Tables.Add(testTable);
            return testDataBase;
        }
    }
}
