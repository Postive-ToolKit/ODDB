using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;

namespace TeamODD.ODDB.Editors.MCP
{
    /// <summary>
    /// Marshals work from background MCP threads onto the Unity main thread.
    /// Required because most Unity APIs (Resources.Load, ScriptableObject access,
    /// AssetDatabase, etc.) reject calls from non-main threads.
    /// </summary>
    public static class McpMainThread
    {
        private static readonly Queue<Action> _queue = new Queue<Action>();
        private static readonly object _gate = new object();
        private static bool _pumpRegistered;

        public static void EnsurePump()
        {
            if (_pumpRegistered) return;
            EditorApplication.update += Pump;
            _pumpRegistered = true;
        }

        public static T Run<T>(Func<T> action)
        {
            T result = default;
            Exception err = null;
            using (var done = new ManualResetEventSlim(false))
            {
                lock (_gate)
                {
                    _queue.Enqueue(() =>
                    {
                        try { result = action(); }
                        catch (Exception ex) { err = ex; }
                        finally { done.Set(); }
                    });
                }
                if (!done.Wait(10_000))
                    throw new TimeoutException("main thread did not respond within 10s");
            }
            if (err != null) throw err;
            return result;
        }

        public static void Run(Action action) => Run<object>(() => { action(); return null; });

        private static void Pump()
        {
            while (true)
            {
                Action work;
                lock (_gate)
                {
                    if (_queue.Count == 0) return;
                    work = _queue.Dequeue();
                }
                try { work(); }
                catch (Exception ex) { McpLog.Error($"main-thread work threw: {ex}"); }
            }
        }
    }
}
