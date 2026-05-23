using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    /// <summary>
    /// Static facade over a single global <see cref="ODDatabase"/> instance.
    /// All caching/business logic lives on the instance — this class only delegates
    /// and owns the global slot + Resources-based asset loading for runtime builds.
    /// </summary>
    public static class ODDBPort
    {
        private static ODDatabase _instance;
        private static bool _isInitialized;

        /// <summary>
        /// The global <see cref="ODDatabase"/> instance backing this facade.
        /// May be null until <see cref="Initialize()"/> has run.
        /// </summary>
        public static ODDatabase Instance => _instance;

        public static bool IsInitialized => _isInitialized;

        #region Initialization

        /// <summary>
        /// Initialize the ODDB system, with an option to force re-initialization.
        /// </summary>
        public static void Initialize(bool isForce)
        {
            if (isForce)
            {
                _isInitialized = false;
                _instance = null;
            }

            Initialize();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            var settings = LoadSettings();
            if (settings == null)
                return;

            if (settings.DisableAutoInitialization)
                return;

            _isInitialized = true;

            var databaseAsset = LoadDatabaseAsset(settings);
            if (databaseAsset == null)
            {
                _instance = new ODDatabase();
                return;
            }

            if (!TryConvertData(databaseAsset.bytes, out _instance))
            {
                ODDB.Logger.Error("Failed to convert database data.");
                _instance = new ODDatabase();
                return;
            }

            ODDB.Logger.Info("ODDB system initialized successfully.");
            try
            {
                _instance.PortData();
                TrimRuntimeMemory();
            }
            catch (InvalidOperationException e)
            {
                ODDB.Logger.Error($"Error during data porting: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes the ODDB system asynchronously, allowing non-blocking database loading.
        /// </summary>
        public static async Task InitializeAsync(bool isForce = false, CancellationToken cancellationToken = default, IProgress<float> progress = null)
        {
            if (_isInitialized && !isForce)
                return;

            if (isForce)
            {
                _isInitialized = false;
                _instance = null;
            }

            _isInitialized = true;
            cancellationToken.ThrowIfCancellationRequested();

            var settings = LoadSettings();
            if (settings == null)
                return;
            progress?.Report(0.1f);

            var databaseAsset = await LoadDatabaseAssetAsync(settings, cancellationToken);
            if (databaseAsset == null)
            {
                ODDB.Logger.Error("Database asset not found.");
                _instance = new ODDatabase();
                return;
            }
            progress?.Report(0.25f);

            cancellationToken.ThrowIfCancellationRequested();

            var binary = databaseAsset.bytes;
            var database = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryConvertData(binary, out var db))
                    return null;
                return db;
            }, cancellationToken);

            if (database == null)
            {
                ODDB.Logger.Error("Failed to convert database data.");
                _instance = new ODDatabase();
                return;
            }
            _instance = database;
            progress?.Report(0.75f);

            cancellationToken.ThrowIfCancellationRequested();

            ODDB.Logger.Info("ODDB system initialized successfully.");
            try
            {
                _instance.PortData();
                TrimRuntimeMemory();
            }
            catch (InvalidOperationException e)
            {
                ODDB.Logger.Error($"Error during data porting: {e.Message}");
                throw;
            }
            progress?.Report(1.0f);
        }

        private static void TrimRuntimeMemory()
        {
            // After porting data to entities, the raw row/cell data is no longer needed at runtime.
            // We keep table structures for field metadata, but individual cells are cleared.
            #if !UNITY_EDITOR
            if (_instance != null)
                _instance.ClearTableData();
            #endif
        }

        private static ODDBRuntimeSettings LoadSettings()
        {
            var settings = Resources.Load<ODDBRuntimeSettings>(nameof(ODDBRuntimeSettings));
            if (settings == null)
                ODDB.Logger.Error("ODDBRuntimeSettings not found in Resources. Please create an ODDBRuntimeSettings asset.");
            return settings;
        }

        private static TextAsset LoadDatabaseAsset(ODDBRuntimeSettings settings)
        {
            var fullPath = Path.Combine(settings.PathFromResources, settings.DBName);
            var filePath = Path.ChangeExtension(fullPath, null);
            var asset = Resources.Load<TextAsset>(filePath);
            if (asset == null)
                ODDB.Logger.Error($"Database asset not found at path: {filePath}");
            return asset;
        }

        private static async Task<TextAsset> LoadDatabaseAssetAsync(ODDBRuntimeSettings settings, CancellationToken cancellationToken)
        {
            var fullPath = Path.Combine(settings.PathFromResources, settings.DBName);
            var filePath = Path.ChangeExtension(fullPath, null);
            var request = Resources.LoadAsync<TextAsset>(filePath);

            while (!request.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            var asset = request.asset as TextAsset;
            if (asset == null)
                ODDB.Logger.Error($"Database asset not found at path: {filePath}");
            return asset;
        }

        private static bool TryConvertData(byte[] binary, out ODDatabase database)
        {
            var importer = new ODDBConverter();
            database = importer.Import(binary);
            return database != null;
        }

        private static ODDatabase EnsureInstance()
        {
            if (_instance == null)
                Initialize();
            return _instance;
        }

        #endregion

        #region Delegated API

        public static void RegisterOnDataPortedCallback(Action callback) =>
            EnsureInstance()?.RegisterOnDataPorted(callback);

        /// <summary>
        /// Enumerates the IDs of entities currently live in the runtime entity cache.
        /// </summary>
        public static IEnumerable<string> GetLiveEntityIds() =>
            _instance != null ? _instance.GetLiveEntityIds() : Enumerable.Empty<string>();

        /// <summary>Get entity of type T by ID.</summary>
        public static T GetEntity<T>(string id) =>
            EnsureInstance() is { } db ? db.GetEntity<T>(id) : default;

        /// <summary>Try to get entity of type T by ID.</summary>
        public static bool TryGetEntity<T>(string id, out T result)
        {
            var db = EnsureInstance();
            if (db == null)
            {
                result = default;
                return false;
            }
            return db.TryGetEntity(id, out result);
        }

        /// <summary>Get all entities of type T.</summary>
        public static IEnumerable<T> GetEntities<T>() =>
            EnsureInstance() is { } db ? db.GetEntities<T>() : Enumerable.Empty<T>();

        /// <summary>Get all entities that implement the specified type.</summary>
        public static IEnumerable<ODDBEntity> GetEntities(Type type) =>
            EnsureInstance() is { } db ? db.GetEntities(type) : Enumerable.Empty<ODDBEntity>();

        #endregion
    }
}
