using System;
using TeamODD.ODDB.Runtime.Attributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TeamODD.ODDB.Runtime.Settings
{
    /// <summary>
    /// Runtime-only ODDB settings — fields needed at game runtime (build-included).
    /// Editor-only fields live on ODDBEditorSettings (added in T19).
    /// </summary>
    public class ODDBRuntimeSettings : ScriptableObject
    {
        /// <summary>Pure read; returns null if the asset doesn't exist yet. No side effects.</summary>
        public static ODDBRuntimeSettings TryLoad()
        {
            return Resources.Load<ODDBRuntimeSettings>("ODDBRuntimeSettings");
        }

        /// <summary>
        /// Resolve the on-disk database path without mutating the global Settings asset.
        /// Falls back to BASE_PATH + default filename when settings are missing or uninitialized.
        /// </summary>
        public static string ResolveDatabasePath() => ResolveDatabasePath(TryLoad());

        public static string ResolveDatabasePath(ODDBRuntimeSettings settings)
        {
            var dbName = string.IsNullOrEmpty(settings?.DBName) ? "ODDB.bytes" : settings.DBName;
            if (settings != null && settings.IsInitialized)
                return System.IO.Path.Combine(settings.Path, dbName);
            return System.IO.Path.Combine(BASE_PATH, dbName);
        }

        public static ODDBRuntimeSettings Setting
        {
            get
            {
                var s = TryLoad();
                if (s != null) return s;
                s = CreateInstance<ODDBRuntimeSettings>();
                s.name = "ODDBRuntimeSettings";
#if UNITY_EDITOR
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateAsset(s, "Assets/Resources/ODDBRuntimeSettings.asset");
                AssetDatabase.SaveAssets();
#endif
                return s;
            }
        }

        public static readonly string BASE_PATH = Application.dataPath + "/Resources";

        public bool IsInitialized => !string.IsNullOrEmpty(_dbPath);
        public bool UseDebugLog => _useDebugLog;
        public bool DisableAutoInitialization => _disableAutoInitialization;
#if ADDRESSABLE_EXIST
        public bool UseAddressableAutoLoad => _useAddressableAutoLoad;
#endif

        public string Path
        {
            get => (Application.dataPath + DBPath).Replace("\\", "/");
            set
            {
                string newVal = value.Replace("\\", "/");
                string dataPath = Application.dataPath.Replace("\\", "/");
                _dbPath = newVal.Replace(dataPath, "");

                string resourcesTag = "/Resources";
                int index = _dbPath.LastIndexOf(resourcesTag, StringComparison.OrdinalIgnoreCase);
                if (index != -1)
                {
                    var resPath = _dbPath.Substring(index + resourcesTag.Length);
                    if (resPath.StartsWith("/")) resPath = resPath.Substring(1);
                    _pathFromResources = resPath;
                }
                else
                {
                    _pathFromResources = _dbPath.StartsWith("/") ? _dbPath.Substring(1) : _dbPath;
                }
            }
        }

        public string FullDBPath => Path + "/" + DBName;
        public string DBPath => _dbPath;
        public string PathFromResources => _pathFromResources;
        public string DBName => _dbName;

        [PathSelector(true)]
        [SerializeField] private string _dbPath;
        [SerializeField] private string _pathFromResources;
        [SerializeField] private string _dbName = "ODDB.bytes";
        [SerializeField] private bool _useDebugLog = false;
        [Tooltip("If true, disables automatic initialization at startup.")]
        [SerializeField] private bool _disableAutoInitialization = false;
#if ADDRESSABLE_EXIST
        [Tooltip("If false, Database returns address of Addressable Asset — not the asset itself.")]
        [SerializeField] private bool _useAddressableAutoLoad = false;
#endif

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(_dbPath))
                Path = _dbPath;
        }
    }
}
