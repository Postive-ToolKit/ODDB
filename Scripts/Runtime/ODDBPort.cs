using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
            _isInitialized = true;

            // Initialize the ODDB system
            // This is where you would set up any necessary configurations or settings
            // for the ODDB system to function correctly.
            _settings = Resources.Load<ODDBSettings>(nameof(ODDBSettings));
            if (_settings == null)
            {
                Debug.LogError("ODDBSettings not found in Resources. Please create an ODDBSettings asset.");
                return;
            }

            var fullPath = Path.Combine(_settings.PathFromResources, _settings.DBName);
            var filePath = Path.ChangeExtension(fullPath, null);
            var databaseAsset = Resources.Load<TextAsset>(filePath);
            if (databaseAsset == null)
            {
                Debug.LogError($"Database asset not found at path: {filePath}");
                return;
            }

            var binary = databaseAsset.bytes;
            if (!TryConvertData(binary, out _database))
            {
                Debug.LogError("Failed to convert database data.");
                return;
            }

            Debug.Log("ODDB system initialized successfully.");
            try
            {
                PortData();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"Error during data porting: {e.Message}");
                throw;
            }
        }

        private static void PortData()
        {
            var tables = _database.Tables;
            foreach (var view in tables.GetAll())
            {
                var targetType = view.BindType;
                if (targetType == null)
                {
                    Debug.LogWarning(
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
                        Debug.LogError($"Failed to create instance of {targetType}");
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
                Debug.LogError("Cannot register a null callback.");
                return;
            }

            _onDataPortedCallbacks.Add(callback);
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
                Debug.LogError($"No entities of ID {id} found.");
                return false;
            }

            if (entity is T typedEntity)
            {
                result = typedEntity;
                return true;
            }

            Debug.LogError(
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
            if (_entityTypeCache.TryGetValue(type, out var cached))
                return cached.Values;

            var newDict = new Dictionary<string, ODDBEntity>();
            foreach (var kvp in _entityTypeCache)
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