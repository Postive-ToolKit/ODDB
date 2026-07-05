using TeamODD.ODDB.Runtime.Attributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TeamODD.ODDB.Editors.Settings
{
    /// <summary>
    /// Editor-only ODDB settings. Stored under Assets/Editor so it never
    /// ships in player builds. Runtime fields live on ODDBRuntimeSettings.
    /// </summary>
    public class ODDBEditorSettings : ScriptableObject
    {
        private const string AssetPath = "Assets/Editor/ODDBEditorSettings.asset";

        /// <summary>Pure read; returns null if the asset doesn't exist yet. No side effects.</summary>
        public static ODDBEditorSettings TryLoad()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<ODDBEditorSettings>(AssetPath);
#else
            return null;
#endif
        }

        public static ODDBEditorSettings Setting
        {
            get
            {
#if UNITY_EDITOR
                var s = TryLoad();
                if (s != null) return s;
                s = CreateInstance<ODDBEditorSettings>();
                s.name = "ODDBEditorSettings";
                if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                    AssetDatabase.CreateFolder("Assets", "Editor");
                AssetDatabase.CreateAsset(s, AssetPath);
                AssetDatabase.SaveAssets();
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
        public string GoogleSheetAPIURL => _googleSheetAPIURL;
        public string GoogleSheetAPISecretKey => _googleSheetAPISecretKey;

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
        [TextArea]
        [Tooltip("The ID of the Google Sheets document to sync with.")]
        [SerializeField] private string _googleSheetAPIURL = string.Empty;
        [Tooltip("API Key for Google Sheets (read-only operations).")]
        [SerializeField] private string _googleSheetAPISecretKey = string.Empty;
    }
}
