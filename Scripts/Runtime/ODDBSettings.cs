using System;
using System.Reflection;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Settings
{
    /// <summary>
    /// DEPRECATED — Phase 4 transitional shim. Routes legacy ODDBSettings.Setting.*
    /// accesses to the new split (ODDBRuntimeSettings + ODDBEditorSettings).
    /// Consumers will be migrated in T20, then this shim is deleted.
    ///
    /// Kept as a ScriptableObject (non-static) so existing code that does
    /// Resources.Load&lt;ODDBSettings&gt;(...) or uses it as a method
    /// parameter/return type still compiles.
    /// </summary>
    [Obsolete("Use ODDBRuntimeSettings.Setting or ODDBEditorSettings.Setting directly.")]
    public class ODDBSettings : ScriptableObject
    {
        public static ODDBSettings Setting
        {
            get
            {
                if (_setting == null)
                    _setting = CreateInstance<ODDBSettings>();
                return _setting;
            }
        }
        private static ODDBSettings _setting;

        public static string BASE_PATH => ODDBRuntimeSettings.BASE_PATH;

        // Runtime delegations
        public bool IsInitialized => ODDBRuntimeSettings.Setting.IsInitialized;
        public bool UseDebugLog => ODDBRuntimeSettings.Setting.UseDebugLog;
        public string Path
        {
            get => ODDBRuntimeSettings.Setting.Path;
            set => ODDBRuntimeSettings.Setting.Path = value;
        }
        public string FullDBPath => ODDBRuntimeSettings.Setting.FullDBPath;
        public string DBPath => ODDBRuntimeSettings.Setting.DBPath;
        public string PathFromResources => ODDBRuntimeSettings.Setting.PathFromResources;
        public string DBName => ODDBRuntimeSettings.Setting.DBName;
        public bool DisableAutoInitialization => ODDBRuntimeSettings.Setting.DisableAutoInitialization;
#if ADDRESSABLE_EXIST
        public bool UseAddressableAutoLoad => ODDBRuntimeSettings.Setting.UseAddressableAutoLoad;
#endif

        // Editor-only delegations. Reflected through ODDBEditorSettings (lives in
        // editor asm) so the Runtime asmdef does not need to reference Editor.
        // Returns defaults outside the Editor.
        public int MaxHistoryCount => GetEditorInt(nameof(MaxHistoryCount), 50);
        public bool UseFirstColumnAsRowName => GetEditorBool(nameof(UseFirstColumnAsRowName), false);
        public string GeneratedCodePath => GetEditorString(nameof(GeneratedCodePath), string.Empty);
        public bool DisableGoogleSheetExport => GetEditorBool(nameof(DisableGoogleSheetExport), false);
        public string GoogleSheetAPIURL => GetEditorString(nameof(GoogleSheetAPIURL), string.Empty);
        public string GoogleSheetAPISecretKey => GetEditorString(nameof(GoogleSheetAPISecretKey), string.Empty);
        public bool EnableMCPServer => GetEditorBool(nameof(EnableMCPServer), true);
        public int MCPServerPort => GetEditorInt(nameof(MCPServerPort), 9123);
        public string MCPServerHost => GetEditorString(nameof(MCPServerHost), "127.0.0.1");
        public bool MCPServerVerbose => GetEditorBool(nameof(MCPServerVerbose), false);

        private static object GetEditorSettingInstance()
        {
#if UNITY_EDITOR
            var type = Type.GetType("TeamODD.ODDB.Editors.Settings.ODDBEditorSettings, TeamODD.ODDB.Editor")
                       ?? Type.GetType("TeamODD.ODDB.Editors.Settings.ODDBEditorSettings, ODDB.Editor")
                       ?? FindTypeAcrossAssemblies("TeamODD.ODDB.Editors.Settings.ODDBEditorSettings");
            if (type == null) return null;
            var prop = type.GetProperty("Setting", BindingFlags.Public | BindingFlags.Static);
            return prop?.GetValue(null);
#else
            return null;
#endif
        }

        private static Type FindTypeAcrossAssemblies(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private static T GetEditorValue<T>(string propName, T fallback)
        {
            var instance = GetEditorSettingInstance();
            if (instance == null) return fallback;
            var prop = instance.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return fallback;
            var v = prop.GetValue(instance);
            return v is T tv ? tv : fallback;
        }

        private static int GetEditorInt(string p, int d) => GetEditorValue(p, d);
        private static bool GetEditorBool(string p, bool d) => GetEditorValue(p, d);
        private static string GetEditorString(string p, string d) => GetEditorValue(p, d);
    }
}
