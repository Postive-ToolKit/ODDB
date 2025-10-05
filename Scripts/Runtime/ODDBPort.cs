using System;
using System.IO;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    public static class ODDBPort
    {
        private static Dictionary<string, ODDBEntity> _entityCache = new();
        private static Dictionary<Type, Dictionary<string,ODDBEntity>> _entityTypeCache = new Dictionary<Type , Dictionary<string,ODDBEntity>>();
        private static List<Action> _onDataPortedCallbacks = new List<Action>();
        private static ODDBSettings _settings;
        private static ODDatabase _database;

        #region Initialization
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
            // Initialize the ODDB system
            // This is where you would set up any necessary configurations or settings
            // for the ODDB system to function correctly.
            _settings = Resources.Load<ODDBSettings>(nameof(ODDBSettings));
            if (_settings == null) {
                Debug.LogError("ODDBSettings not found in Resources. Please create an ODDBSettings asset.");
                return;
            }

            var fullPath = Path.Combine(_settings.PathFromResources, _settings.DBName);
            var filePath = Path.ChangeExtension(fullPath, null);
            var databaseAsset = Resources.Load<TextAsset>(filePath);
            if (databaseAsset == null) {
                Debug.LogError($"Database asset not found at path: {filePath}");
                return;
            }
            var xml = databaseAsset.text;
            if (!TryConvertData(xml, out _database))
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
                    Debug.LogError($"BindType is null for table {view.Name} with key {view.ID}, table will be excluded.");
                    continue;
                }
                
                if(view is not Table table)
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
                    entity.Import(table.TotalFields,row);
                    _entityCache[row.ID] = entity;
                    _entityTypeCache[targetType][row.ID] = entity;
                }
            }

            foreach (var callback in _onDataPortedCallbacks)
            {
                callback?.Invoke();
            }
        }
        private static bool TryConvertData(string xml, out ODDatabase database)
        {
            var importer = new ODDBConverter();
            database = importer.CreateDatabase(xml);
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
        public static T GetEntity<T>(string id) where T : ODDBEntity
        {
            if (string.IsNullOrEmpty(id))
                return null;
            
            var type = typeof(T);
            if (_entityCache.TryGetValue(id, out var entity))
            {
                if (entity is T typedEntity)
                {
                    return typedEntity;
                }
                Debug.LogError($"Entity with ID {id} is not of type {type}. Found type: {entity.GetType()}");
                return null;
            }
            Debug.LogError($"No entities of ID {id} found.");
            return null;
        }
        public static IEnumerable<T> GetEntities<T>() where T : ODDBEntity
        {
            var type = typeof(T);
            if (_entityTypeCache.ContainsKey(type))
            {
                return _entityTypeCache[type] as IEnumerable<T>;
            }
            Debug.LogError($"No entities of type {type} found.");
            return null;
        }
    }
}