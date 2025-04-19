using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Utils;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    public static class ODDBPort
    {
        private static Dictionary<Type, List<ODDBEntity>> _entityCache = new Dictionary<Type, List<ODDBEntity>>();
        private static Dictionary<Type, Dictionary<string,ODDBEntity>> _entityIdCache = new Dictionary<Type , Dictionary<string,ODDBEntity>>();
        private static ODDBSettings _settings;
        private static ODDatabase _database;
        #region Initialization
        [RuntimeInitializeOnLoadMethod]
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
            foreach (var table in tables)
            {
                var targetType = table.BindType;
                if (targetType == null)
                {
                    Debug.LogError($"BindType is null for table {table.Name} with key {table.Key}, table will be excluded.");
                    continue;
                }

                if (!_entityCache.ContainsKey(targetType))
                {
                    _entityCache[targetType] = new List<ODDBEntity>();
                    _entityIdCache[targetType] = new Dictionary<string, ODDBEntity>();
                }
                    
                foreach (var row in table.ReadOnlyRows)
                {
                    var entity = Activator.CreateInstance(targetType) as ODDBEntity;
                    if (entity == null)
                    {
                        Debug.LogError($"Failed to create instance of {targetType}");
                        continue;
                    }
                    entity.Import(table.TableMetas,row);
                    _entityCache[targetType].Add(entity);
                    _entityIdCache[targetType][row.Key] = entity;
                }
            }
        }
        private static bool TryConvertData(string xml, out ODDatabase database)
        {
            database = null;
            var serializer = new XmlSerializer(typeof(ODDatabaseDTO));
            using var stringReader = new StringReader(xml);
            var databaseDto = (ODDatabaseDTO)serializer.Deserialize(stringReader);
        
            var importer = new ODDBImporter();
            database = importer.CreateDatabase(databaseDto);
            
            return database != null;
        }
        #endregion
        public static T GetEntity<T>(int index = 0) where T : ODDBEntity
        {
            var type = typeof(T);
            if (_entityCache.ContainsKey(type))
            {
                var entities = _entityCache[type];
                if (index < entities.Count)
                {
                    return (T)entities[index];
                }
                Debug.LogError($"Index {index} out of range for type {type}");
                return null;
            }
            Debug.LogError($"No entities of type {type} found.");
            return null;
        }

        public static T GetEntity<T>(string id) where T : ODDBEntity
        {
            var type = typeof(T);
            if (_entityIdCache.ContainsKey(type))
            {
                var entities = _entityIdCache[type];
                if (entities.ContainsKey(id))
                {
                    return (T)entities[id];
                }

                Debug.LogError($"ID {id} not found for type {type}");
                return null;
            }

            Debug.LogError($"No entities of type {type} found.");
            return null;
        }
        public static IEnumerable<T> GetEntities<T>() where T : ODDBEntity
        {
            var type = typeof(T);
            if (_entityCache.ContainsKey(type))
            {
                return _entityCache[type] as IEnumerable<T>;
            }
            Debug.LogError($"No entities of type {type} found.");
            return null;
        }
    }
}