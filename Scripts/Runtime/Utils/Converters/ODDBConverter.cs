using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.DTO;
using UnityEngine;


namespace TeamODD.ODDB.Runtime.Utils.Converters
{
    public class ODDBConverter
    {
        public static readonly List<DataBaseCreateEvent> OnDatabaseCreated = new List<DataBaseCreateEvent>();
        public static event Action<ODDatabase> OnDatabaseExported;
        public ODDatabase CreateDatabase(string data)
        {
            var databaseDto = new DatabaseDTO();
            try
            {
                databaseDto = JsonConvert.DeserializeObject<DatabaseDTO>(data);
            }
            catch (Exception e)
            {
                Debug.LogError("ODDBConverter.CreateDatabase failed to deserialize database - use default database. Error: " + e);
            }
            
            var database = new ODDatabase();
            database.FromDTO(databaseDto);
            
            OnDatabaseCreated.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            
            Debug.Log($"ODDBConverter.CreateDatabase Invoking {OnDatabaseCreated.Count} OnDatabaseCreated events");

            foreach (var createEvent in OnDatabaseCreated)
            {
                Debug.Log($"ODDBConverter.CreateDatabase Invoking OnDatabaseCreated event: with priority {createEvent.Priority}");
                createEvent.OnEvent?.Invoke(database);
            }
            
            OnDatabaseCreated.Clear();
            return database;
        }
        
        public string Export(ODDatabase database)
        {
            Assert.IsNotNull(database, "ODDBConverter.Export database is null");
            var dto = database.ToDTO();
            var data = JsonConvert.SerializeObject(dto, Formatting.Indented);
            OnDatabaseExported?.Invoke(database);
            return data;
        }
    }
}