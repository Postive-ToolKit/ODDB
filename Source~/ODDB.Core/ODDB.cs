using System;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Runtime.Async;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Logging;
using TeamODD.ODDB.Runtime.Types;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Runtime
{
    public static class ODDB
    {
        private static readonly object AsyncLoaderLock = new object();
        private static IAsyncLoader _asyncLoader;

        /// <summary>
        /// Active logger slot. Defaults to NullLogger; Unity-side bootstrap installs a UnityLogger.
        /// </summary>
        public static IODDBLogger Logger { get; set; } = new NullLogger();

        /// <summary>
        /// Verbose runtime debug log toggle. Mirrors ODDBRuntimeSettings.UseDebugLog
        /// for Core code that cannot reference the Unity-side ScriptableObject.
        /// </summary>
        public static bool DebugLog { get; set; } = false;

        public static bool HasAsyncLoader
        {
            get
            {
                lock (AsyncLoaderLock)
                    return _asyncLoader != null;
            }
        }

        public static bool TryRegisterAsyncLoader(IAsyncLoader loader)
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));

            lock (AsyncLoaderLock)
            {
                if (_asyncLoader != null)
                    return false;
                _asyncLoader = loader;
                return true;
            }
        }

        public static void RegisterAsyncLoader(IAsyncLoader loader, bool replaceExisting = false)
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));

            lock (AsyncLoaderLock)
            {
                if (_asyncLoader != null && !ReferenceEquals(_asyncLoader, loader) && !replaceExisting)
                    throw new InvalidOperationException("An ODDB async loader is already registered.");

                _asyncLoader = loader;
            }
        }

        public static bool UnregisterAsyncLoader(IAsyncLoader loader)
        {
            if (loader == null) return false;

            lock (AsyncLoaderLock)
            {
                if (!ReferenceEquals(_asyncLoader, loader)) return false;
                _asyncLoader = null;
                return true;
            }
        }

        public static Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            IAsyncLoader loader;
            lock (AsyncLoaderLock)
                loader = _asyncLoader;

            if (loader == null)
                throw new InvalidOperationException(
                    "No ODDB async loader is registered. Call ODDB.RegisterAsyncLoader first.");

            return loader.GetAsync<T>(key, cancellationToken);
        }

        public static void Release<T>(T asset)
        {
            IAsyncLoader loader;
            lock (AsyncLoaderLock)
                loader = _asyncLoader;

            if (loader == null)
                throw new InvalidOperationException(
                    "No ODDB async loader is registered. Call ODDB.RegisterAsyncLoader first.");

            loader.Release(asset);
        }

        /// <summary>
        /// Clears engine-agnostic static caches and pending conversion callbacks.
        /// Unity-side lifecycle hooks call this when entering play mode or reloading assemblies.
        /// The registered async loader and logger are intentionally preserved.
        /// </summary>
        public static void ResetRuntimeState()
        {
            ODDBID.ClearTrackedIds();
            ODDBTypeUtility.ResetCache();
            ODDBEnumUtility.ResetCache();
            TypeRegistry.ResetCache();
            ODDBEntity.ResetFieldCache();
            ODDBConverter.ResetRuntimeState();
        }
    }
}
