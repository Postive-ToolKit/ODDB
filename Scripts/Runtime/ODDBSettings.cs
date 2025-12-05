using TeamODD.ODDB.Runtime.Attributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TeamODD.ODDB.Runtime.Settings
{
    public class ODDBSettings : ScriptableObject
    {
        public static ODDBSettings Setting
        {
            get
            {
                var settingFiles = Resources.Load<ODDBSettings>("ODDBSettings");
                if (settingFiles == null)
                {
                    settingFiles = CreateInstance<ODDBSettings>();
                    settingFiles.name = "ODDBSettings";
                    // find resources folder
                    #if UNITY_EDITOR
                    if (!AssetDatabase.IsValidFolder("Assets/Resources")) {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    AssetDatabase.CreateAsset(settingFiles, "Assets/Resources/ODDBSettings.asset");
                    AssetDatabase.SaveAssets();
                    #endif
                }

                return settingFiles;
            }
        }
        public static readonly string BASE_PATH = Application.dataPath + "/Resources";
        public bool IsInitialized => _isInitialized;
        
        public bool UseDebugLog => _useDebugLog;
        
        public string Path {
            get => Application.dataPath + DBPath;
            set{
                _dbPath = value.Replace(Application.dataPath,"");
                var curPath = _dbPath.Replace("/Resources", "");
                if (curPath.StartsWith("/"))
                    curPath = curPath.Substring(1);
                _pathFromResources = curPath;
                _isInitialized = true;
            }
        }
        public string FullDBPath => Path + "/" + DBName;
        public string DBPath => _dbPath;
        public string PathFromResources => _pathFromResources;
        public string DBName => _dbName;
        public int MaxHistoryCount => _maxHistoryCount;
        
        public string GoogleSheetAPIURL => _googleSheetAPIURL;
        public string GoogleSheetAPISecretKey => _googleSheetAPISecretKey;
        public bool DisableGoogleSheetExport => _disableGoogleSheetExport;
        
        #if ADDRESSABLE_EXIST
        public bool UseAddressableAutoLoad => _useAddressableAutoLoad;
        #endif
        
        [HideInInspector] private bool _isInitialized = false;
        [SerializeField] private bool _useDebugLog = false;
        [PathSelector(true)]
        [SerializeField] private string _dbPath;
        [SerializeField] private string _pathFromResources;
        [SerializeField] private string _dbName = "ODDB.bytes";
        [Space(10)]
        [Header("Editor Settings")]
        [Tooltip("The maximum number of history items to keep in the undo stack.")]
        [SerializeField, Min(1)] private int _maxHistoryCount = 50;
        
        [Space(10)]
        [Header("Google Sheets Settings")]
        [SerializeField] private bool _disableGoogleSheetExport = false;
        [TextArea]
        [Tooltip("The ID of the Google Sheets document to sync with.")]
        [SerializeField] private string _googleSheetAPIURL = string.Empty;
        [Tooltip("API Key for Google Sheets (read-only operations).")]
        [SerializeField] private string _googleSheetAPISecretKey = string.Empty;
        #if ADDRESSABLE_EXIST
        [Space(10)]
        [Header("Resource Settings")]
        [Tooltip("If false, Database will return address of Addressable Asset - Not the asset itself.")]
        [SerializeField] private bool _useAddressableAutoLoad = false;
        #endif
        private void OnValidate()
        {
            if(!string.IsNullOrEmpty(_dbPath))
            {
                Path = _dbPath;
            }
        }
    }
}