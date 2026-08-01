using System;
using TeamODD.ODDB.Runtime.Attributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TeamODD.ODDB.Editors.Settings
{
    /// <summary>
    /// Editor-only ODDB settings. Resolved through AssetDatabase by type so
    /// the settings asset can be moved without breaking lookups.
    /// Runtime fields live on ODDBRuntimeSettings.
    /// </summary>
    public class ODDBEditorSettings : ScriptableObject
    {
        private const string DefaultFolderPath = "Assets/Settings";
        private const string DefaultAssetPath = DefaultFolderPath + "/ODDBEditorSettings.asset";
        private const string LegacyAssetPath = "Assets/Editor/ODDBEditorSettings.asset";
        private static ODDBEditorSettings _cachedSetting;

        /// <summary>Pure read; returns null if the asset doesn't exist yet. No side effects.</summary>
        public static ODDBEditorSettings TryLoad()
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets($"t:{nameof(ODDBEditorSettings)}");
            ODDBEditorSettings fallback = null;
            var fallbackPath = string.Empty;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<ODDBEditorSettings>(path);
                if (settings == null)
                    continue;

                if (path == DefaultAssetPath)
                    return settings;

                if (fallback == null || IsPreferredFallback(path, fallbackPath))
                {
                    fallback = settings;
                    fallbackPath = path;
                }
            }

            return fallback;
#else
            return null;
#endif
        }

        public static ODDBEditorSettings Setting
        {
            get
            {
#if UNITY_EDITOR
                // AssetDatabase.FindAssets is project-wide and this property is read
                // while binding every visible view-reference cell. Unity keeps the
                // ScriptableObject instance updated when its asset changes, so reuse
                // it until it is deleted or the domain reloads.
                if (_cachedSetting != null)
                    return _cachedSetting;

                var s = TryLoad();
                if (s != null)
                {
                    _cachedSetting = s;
                    return s;
                }
                s = CreateInstance<ODDBEditorSettings>();
                s.name = "ODDBEditorSettings";
                EnsureFolder(DefaultFolderPath);
                AssetDatabase.CreateAsset(s, DefaultAssetPath);
                AssetDatabase.SaveAssets();
                _cachedSetting = s;
                return s;
#else
                return null;
#endif
            }
        }

        public int MaxHistoryCount => _maxHistoryCount;
        public bool UseFirstColumnAsRowName => _useFirstColumnAsRowName;
        public string GeneratedCodePath => _generatedCodePath;
        public bool DisableGoogleSheetExport => _disableGoogleSheetExport;
        public string GoogleSpreadsheetId => _googleSpreadsheetId;
        public bool ConfirmGoogleSheetColumnDeletion => _confirmGoogleSheetColumnDeletion;

        public bool EnableMCPServer => _enableMCPServer;
        public int MCPServerPort => _mcpServerPort;
        public string MCPServerHost => _mcpServerHost;
        public bool MCPServerVerbose => _mcpServerVerbose;

        [Header("Editor Settings")]
        [Tooltip("The maximum number of history items to keep in the undo stack.")]
        [SerializeField, Min(1)] private int _maxHistoryCount = 50;
        [Tooltip("Use the first column of the row as the row name when show dropdown selector in the editor.")]
        [SerializeField] private bool _useFirstColumnAsRowName = false;

        [Space(10)]
        [Header("Code Generation")]
        [Tooltip("Output folder for generated POCO classes (Assets-relative). Leave empty to disable code generation.")]
        [PathSelector(true)]
        [SerializeField] private string _generatedCodePath = string.Empty;

        [Space(10)]
        [Header("MCP Server")]
        [Tooltip("Enable the in-Editor MCP server that exposes ODDB to AI clients via HTTP.")]
        [SerializeField] private bool _enableMCPServer = true;
        [Tooltip("TCP port for the MCP HTTP server. If busy, ODDB retries this same port instead of switching ports.")]
        [SerializeField] private int _mcpServerPort = 9123;
        [Tooltip("Bind host. 127.0.0.1 keeps the server loopback-only.")]
        [SerializeField] private string _mcpServerHost = "127.0.0.1";
        [Tooltip("Log every MCP call to the Unity console.")]
        [SerializeField] private bool _mcpServerVerbose = false;

        [Space(10)]
        [Header("Google Sheets Settings")]
        [SerializeField] private bool _disableGoogleSheetExport = false;
        [Tooltip("The spreadsheet ID from the Google Sheets document URL.")]
        [SerializeField] private string _googleSpreadsheetId = string.Empty;
        [Tooltip("Ask for confirmation before an export physically deletes ODDB-managed columns.")]
        [SerializeField] private bool _confirmGoogleSheetColumnDeletion = true;

        // Kept serialized for one migration cycle so existing assets do not lose
        // their values before users finish moving away from Apps Script.
        [SerializeField, HideInInspector] private string _googleSheetAPIURL = string.Empty;
        [SerializeField, HideInInspector] private string _googleSheetAPISecretKey = string.Empty;

#if UNITY_EDITOR
        private static void EnsureFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static bool IsPreferredFallback(string path, string currentPath)
        {
            if (path == LegacyAssetPath)
                return currentPath != LegacyAssetPath;

            if (currentPath == LegacyAssetPath)
                return false;

            return string.Compare(path, currentPath, StringComparison.OrdinalIgnoreCase) < 0;
        }
#endif
    }
}
