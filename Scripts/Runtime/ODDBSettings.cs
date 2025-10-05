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
                var curPath = _dbPath.Replace("/Resources/", "");
                _pathFromResources = curPath;
                _isInitialized = true;
            }
        }
        public string DBPath => _dbPath;
        public string PathFromResources => _pathFromResources;
        public string DBName => _dbName;
        public int IncrementCount => _incrementCount;
        public string GoogleSheetsID => _googleSheetsID;
        public string GoogleSheetApiKey => _googleSheetApiKey;
        public string GoogleOAuthClientID => _googleOAuthClientID;
        public string GoogleOAuthClientSecret => _googleOAuthClientSecret;
        [HideInInspector] private bool _isInitialized = false;
        [SerializeField] private bool _useDebugLog = false;
        [PathSelector(true)]
        [SerializeField] private string _dbPath;
        [SerializeField] private string _pathFromResources;
        [SerializeField] private string _dbName = "ODDB.json";
        [Space(10)]
        [Header("Increment Settings")]
        [Tooltip("When adding new items to the database, how many items to add at once.")]
        [SerializeField, Min(1)] private int _incrementCount = 10;
        [Space(10)]
        [Header("Google Sheets Settings")]
        [Tooltip("The ID of the Google Sheets document to sync with.")]
        [SerializeField] private string _googleSheetsID = "";
        [Tooltip("API Key for Google Sheets (read-only operations).")]
        [SerializeField] private string _googleSheetApiKey = "";
        
        [Space(5)]
        [Header("Google OAuth2 Settings (for write operations)")]
        [Tooltip("OAuth2 Client ID from Google Cloud Console.")]
        [SerializeField] private string _googleOAuthClientID = "";
        [Tooltip("OAuth2 Client Secret from Google Cloud Console.")]
        [SerializeField] private string _googleOAuthClientSecret = "";
        private void OnValidate()
        {
            if(!string.IsNullOrEmpty(_dbPath))
            {
                Path = _dbPath;
            }
        }
    }
}