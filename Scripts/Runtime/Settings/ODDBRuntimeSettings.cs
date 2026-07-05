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
            get => ToAbsolutePath(DBPath);
            set
            {
                _dbPath = ToDataPathRelativePath(value);
                _pathFromResources = ToResourcesRelativePath(_dbPath);
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

        private static string ToAbsolutePath(string path)
        {
            var normalized = NormalizeSlashes(path);
            if (string.IsNullOrEmpty(normalized))
                return NormalizeSlashes(BASE_PATH);

            var dataPath = NormalizeSlashes(Application.dataPath).TrimEnd('/');
            if (IsSameOrChildPath(normalized.TrimEnd('/'), dataPath) || IsFilesystemRootedPath(normalized))
                return normalized;

            if (normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase))
                return dataPath;

            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                var projectRoot = System.IO.Directory.GetParent(Application.dataPath)?.FullName
                    ?? System.IO.Directory.GetCurrentDirectory();
                return NormalizeSlashes(System.IO.Path.GetFullPath(System.IO.Path.Combine(projectRoot, normalized)));
            }

            if (normalized.StartsWith("/"))
                return dataPath + normalized;

            return dataPath + "/" + normalized;
        }

        private static string ToDataPathRelativePath(string path)
        {
            var normalized = NormalizeSlashes(path).TrimEnd('/');
            if (string.IsNullOrEmpty(normalized))
                return string.Empty;

            var dataPath = NormalizeSlashes(Application.dataPath).TrimEnd('/');
            if (IsSameOrChildPath(normalized, dataPath))
                return normalized.Substring(dataPath.Length);

            if (normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return normalized.Substring("Assets".Length);

            return normalized;
        }

        private static string ToResourcesRelativePath(string path)
        {
            var normalized = NormalizeSlashes(path);
            const string resourcesTag = "/Resources";
            int index = normalized.LastIndexOf(resourcesTag, StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                var resPath = normalized.Substring(index + resourcesTag.Length);
                return resPath.StartsWith("/") ? resPath.Substring(1) : resPath;
            }

            if (normalized.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
                return normalized.Substring("Resources/".Length);

            return normalized.StartsWith("/") ? normalized.Substring(1) : normalized;
        }

        private static string NormalizeSlashes(string path)
        {
            return string.IsNullOrEmpty(path) ? path : path.Replace("\\", "/");
        }

        private static bool IsSameOrChildPath(string path, string parentPath)
        {
            return string.Equals(path, parentPath, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(parentPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFilesystemRootedPath(string path)
        {
            return path.StartsWith("//", StringComparison.Ordinal)
                || (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '/')
                || (path.StartsWith("/", StringComparison.Ordinal) && System.IO.Directory.Exists(path));
        }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(_dbPath))
                Path = _dbPath;
        }
    }
}
