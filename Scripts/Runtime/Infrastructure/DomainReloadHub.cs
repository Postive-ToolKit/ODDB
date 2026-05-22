using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Infrastructure
{
    public static class DomainReloadHub
    {
        private static readonly HashSet<IClearableCache> _caches = new HashSet<IClearableCache>();

        public static int RegisteredCount => _caches.Count;

        public static void Register(IClearableCache cache)
        {
            if (cache == null) return;
            _caches.Add(cache);
        }

        public static void Unregister(IClearableCache cache)
        {
            if (cache == null) return;
            _caches.Remove(cache);
        }

        public static void TriggerClearAll()
        {
            foreach (var cache in _caches)
            {
                try
                {
                    cache.Clear();
                }
                catch (Exception e)
                {
                    ODDB.Logger.Error(e.ToString());
                }
            }
#if ODDB_DEBUG_RELOAD
            ODDB.Logger.Info($"[DomainReloadHub] TriggerClearAll invoked (count={_caches.Count})");
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnRuntimeInitialize()
        {
            TriggerClearAll();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnEditorInitialize()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += TriggerClearAll;
        }
#endif
    }
}
