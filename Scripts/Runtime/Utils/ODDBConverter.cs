using TeamODD.ODDB.Runtime.Data;
using UnityEngine;


namespace TeamODD.ODDB.Runtime.Utils
{
    public class ODDBConverter
    {
        public ODDatabase CreateDatabase(string data)
        {
            var database = new ODDatabase();
            database.TryDeserialize(data);
            return database;
        }
        
        public string Export(ODDatabase database)
        {
            if (database == null)
            {
                Debug.LogError("ODDBExporter.Export cannot export null database");
                return default;
            }

            if (!database.TrySerialize(out var data))
                return null;
            return data;
        }
    }
}