using UnityEngine;

namespace TeamODD.ODDB.Runtime.Logging
{
    public static class UnityLoggerBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Install()
        {
            if (ODDB.Logger is NullLogger) ODDB.Logger = new UnityLogger();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void InstallEditor()
        {
            if (ODDB.Logger is NullLogger) ODDB.Logger = new UnityLogger();
        }
#endif
    }
}
