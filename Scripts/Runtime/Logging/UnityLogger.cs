using UnityEngine;

namespace TeamODD.ODDB.Runtime.Logging
{
    public class UnityLogger : IODDBLogger
    {
        public void Info(string msg)  => Debug.Log($"[ODDB] {msg}");
        public void Warn(string msg)  => Debug.LogWarning($"[ODDB] {msg}");
        public void Error(string msg) => Debug.LogError($"[ODDB] {msg}");
    }
}
