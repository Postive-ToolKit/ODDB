using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    public static class ODDBPort
    {
        public static bool IsInitialized => _isInitialized;
        private static readonly Dictionary<string, ODDBEntity> _entityCache = new();

        private static readonly Dictionary<Type, Dictionary<string, ODDBEntity>> _entityTypeCache =
            new Dictionary<Type, Dictionary<string, ODDBEntity>>();

        private static readonly List<Action> _onDataPortedCallbacks = new List<Action>();
        private static ODDBSettings _settings;
        private static ODDatabase _database;
        private static bool _isInitialized = false;

        #region Initialization

        /// <summary>
        /// Initialize the ODDB system, with an option to force re-initialization.
        /// For Editor Initialization.
        /// </summary>
        /// <param name="isForce"> If true, forces re-initialization even if already initialized. </param>
        public static void Initialize(bool isForce)
        {
            if (isForce)
            {
                _isInitialized = false;
                _entityCache.Clear();
                _entityTypeCache.Clear();
                _onDataPortedCallbacks.Clear();
            }

            Initialize();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            _settings = LoadSettings();
            if (_settings == null)
                return;

            if (_settings.DisableAutoInitialization)
                return;

            _isInitialized = true;

            var databaseAsset = LoadDatabaseAsset(_settings);
            if (databaseAsset == null)
                return;

            if (!TryConvertData(databaseAsset.bytes, out _database))
            {
                ODDB.Logger.Error("Failed to convert database data.");
                return;
            }

            ODDB.Logger.Info("ODDB system initialized successfully.");
            try
            {
                PortData();
            }
            catch (InvalidOperationException e)
            {
                ODDB.Logger.Error($"Error during data porting: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes the ODDB system asynchronously, allowing non-blocking database loading.
        /// Use this when you want to avoid startup hitches with large databases.
        /// </summary>
        /// <param name="isForce">If true, forces re-initialization even if already initialized.</param>
        /// <param name="cancellationToken">Token to cancel the async operation.</param>
        /// <param name="progress">Reports initialization progress (0.0 to 1.0).</param>
        public static async Task InitializeAsync(bool isForce = false, CancellationToken cancellationToken = default, IProgress<float> progress = null)
        {
            if (_isInitialized && !isForce)
                return;

            if (isForce)
            {
                _isInitialized = false;
                _entityCache.Clear();
                _entityTypeCache.Clear();
                _onDataPortedCallbacks.Clear();
            }

            _isInitialized = true;
            cancellationToken.ThrowIfCancellationRequested();

            _settings = LoadSettings();
            if (_settings == null)
            {
                ODDB.Logger.Error("ODDBSettings not found in Resources. Please create an ODDBSettings asset.");
                return;
            }
            progress?.Report(0.1f);

            var databaseAsset = await LoadDatabaseAssetAsync(_settings, cancellationToken);
            if (databaseAsset == null)
            {
                ODDB.Logger.Error("Database asset not found.");
                return;
            }
            progress?.Report(0.25f);

            cancellationToken.ThrowIfCancellationRequested();

            // Decompression + deserialization can run on a background thread
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
                return;
            }
            _database = database;
            progress?.Report(0.75f);

            cancellationToken.ThrowIfCancellationRequested();

            // Entity creation must run on the main thread (Unity/Reflection restrictions)
            ODDB.Logger.Info("ODDB system initialized successfully.");
            try
            {
                PortData();
            }
            catch (InvalidOperationException e)
            {
                ODDB.Logger.Error($"Error during data porting: {e.Message}");
                throw;
            }
            progress?.Report(1.0f);
        }

        private static void PortData()
        {
            var tables = _database.Tables;
            foreach (var view in tables.GetAll())
            {
                var targetType = view.BindType;
                if (targetType == null)
                {
                    ODDB.Logger.Warn(
                        $"BindType is null for table {view.Name} with key {view.ID}, table will be excluded.");
                    continue;
                }

                if (view is not Table table)
                    return;

                if (!_entityTypeCache.ContainsKey(targetType))
                {
                    _entityTypeCache[targetType] = new Dictionary<string, ODDBEntity>();
                }

                foreach (var row in table.Rows)
                {
                    var entity = Activator.CreateInstance(targetType) as ODDBEntity;
                    if (entity == null)
                    {
                        ODDB.Logger.Error($"Failed to create instance of {targetType}");
                        continue;
                    }

                    entity.Import(table.TotalFields, row);
                    _entityCache[row.ID] = entity;
                    _entityTypeCache[targetType][row.ID] = entity;
                }
            }

            foreach (var callback in _onDataPortedCallbacks)
            {
                callback?.Invoke();
            }

            // Memory Trimming: After porting data to entities, 
            // the raw row/cell data in _database is no longer needed at runtime.
            #if !UNITY_EDITOR
            if (_database != null)
            {
                _database.ClearTableData();
                // We keep the _database and table structures for field metadata, 
                // but individual cells are cleared.
            }
            #endif
        }

        private static ODDBSettings LoadSettings()
        {
            var settings = Resources.Load<ODDBSettings>(nameof(ODDBSettings));
            if (settings == null)
                ODDB.Logger.Error("ODDBSettings not found in Resources. Please create an ODDBSettings asset.");
            return settings;
        }

        private static TextAsset LoadDatabaseAsset(ODDBSettings settings)
        {
            var fullPath = Path.Combine(settings.PathFromResources, settings.DBName);
            var filePath = Path.ChangeExtension(fullPath, null);
            var asset = Resources.Load<TextAsset>(filePath);
            if (asset == null)
                ODDB.Logger.Error($"Database asset not found at path: {filePath}");
            return asset;
        }

        private static async Task<TextAsset> LoadDatabaseAssetAsync(ODDBSettings settings, CancellationToken cancellationToken)
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

        public static void RegisterOnDataPortedCallback(Action callback)
        {
            if (callback == null)
            {
                ODDB.Logger.Error("Cannot register a null callback.");
                return;
            }

            _onDataPortedCallbacks.Add(callback);
        }

        /// <summary>
        /// Enumerates the IDs of entities currently live in the runtime entity cache.
        /// Used by ODDBID's domain-reload rebuild so the collision-avoidance set can be
        /// repopulated from still-loaded entities rather than cleared outright.
        /// </summary>
        public static IEnumerable<string> GetLiveEntityIds()
        {
            return _entityCache.Keys;
        }

        #endregion

        /// <summary>
        /// Get entity of type T by ID
        /// </summary>
        /// <param name="id"> The ID of the entity </param>
        /// <typeparam name="T"> The type of the entity to retrieve </typeparam>
        /// <returns> The entity of type T with the specified ID, or default if not found </returns>
        public static T GetEntity<T>(string id)
        {
            if (TryGetEntity<T>(id, out var entity))
                return entity;
            return default;
        }

        /// <summary>
        /// Try to get entity of type T by string value
        /// </summary>
        /// <param name="id"> The ID of the entity </param>
        /// <param name="result"> The output entity if found </param>
        /// <typeparam name="T"> The type of the entity to retrieve </typeparam>
        /// <returns></returns>
        public static bool TryGetEntity<T>(string id, out T result)
        {
            result = default;
            if (string.IsNullOrEmpty(id))
                return false;

            if (_entityCache.TryGetValue(id, out var entity) == false)
            {
                ODDB.Logger.Error($"No entities of ID {id} found.");
                return false;
            }

            if (entity is T typedEntity)
            {
                result = typedEntity;
                return true;
            }

            ODDB.Logger.Error(
                $"Entity with ID {id} does not implement interface {typeof(T)}. Found type: {entity.GetType()}");
            return false;
        }
        
        /// <summary>
        /// Get all entities of type T
        /// </summary>
        /// <typeparam name="T"> The type of entities to retrieve </typeparam>
        /// <returns> All entities of type T </returns>
        public static IEnumerable<T> GetEntities<T>()
        {
            var type = typeof(T);
            var entities = GetEntities(type);
            return entities.OfType<T>();
        }

        /// <summary>
        /// Get all entities that implement the specified interface type
        /// </summary>
        /// <param name="type"> The interface type </param>
        /// <returns> All entities that implement the specified interface type </returns>
        public static IEnumerable<ODDBEntity> GetEntities(Type type)
        {
            if (type == null)
                return Enumerable.Empty<ODDBEntity>();

            var newDict = new Dictionary<string, ODDBEntity>();
            foreach (var kvp in _entityTypeCache.ToList())
            {
                if (type.IsAssignableFrom(kvp.Key) == false)
                    continue;
                foreach (var entity in kvp.Value.Values)
                    newDict[entity.ID] = entity;
            }

            _entityTypeCache[type] = newDict;
            return newDict.Values;
        }
    }
}
