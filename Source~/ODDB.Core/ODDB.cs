using TeamODD.ODDB.Runtime.Logging;

namespace TeamODD.ODDB.Runtime
{
    public static class ODDB
    {
        /// <summary>
        /// Active logger slot. Defaults to NullLogger; Unity-side bootstrap installs a UnityLogger.
        /// </summary>
        public static IODDBLogger Logger { get; set; } = new NullLogger();

        /// <summary>
        /// Verbose runtime debug log toggle. Mirrors ODDBRuntimeSettings.UseDebugLog
        /// for Core code that cannot reference the Unity-side ScriptableObject.
        /// </summary>
        public static bool DebugLog { get; set; } = false;
    }
}
