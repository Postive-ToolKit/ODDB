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
            return Run(action, 10_000);
        }

        internal static T Run<T>(Func<T> action, int timeoutMilliseconds)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));

            T result = default;
            Exception err = null;
            var stateGate = new object();
            var started = false;
            var cancelled = false;
            using (var done = new ManualResetEventSlim(false))
            {
                lock (_gate)
                {
                    _queue.Enqueue(() =>
                    {
                        lock (stateGate)
                        {
                            if (cancelled)
                                return;
                            started = true;
                        }

                        try { result = action(); }
                        catch (Exception ex) { err = ex; }
                        finally { done.Set(); }
                    });
                }
                // Nudge the editor so EditorApplication.update fires even when
                // the editor window is unfocused. Delegate concat is atomic, so
                // this is safe to call from a background thread.
                try { EditorApplication.delayCall += NoOp; } catch { }
                if (!done.Wait(timeoutMilliseconds))
                {
                    lock (stateGate)
                    {
                        if (!started)
                        {
                            cancelled = true;
                            throw new TimeoutException(
                                $"main thread did not respond within {timeoutMilliseconds}ms");
                        }
                    }

                    // The action already started. Waiting for its real result preserves
                    // exactly-once semantics instead of reporting failure while a
                    // mutation is still running and encouraging a duplicate retry.
                    done.Wait();
                }
            }
            if (err != null) throw err;
            return result;
        }

        private static void NoOp() { }

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

        internal static void PumpPendingForTesting() => Pump();

        internal static int PendingCountForTesting
        {
            get
            {
                lock (_gate)
                    return _queue.Count;
            }
        }

        internal static void ResetForTesting()
        {
            lock (_gate)
                _queue.Clear();
        }
    }
}
