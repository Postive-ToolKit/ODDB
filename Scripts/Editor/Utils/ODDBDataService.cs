using System.IO;
using System.Xml.Serialization;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Utils;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Utils
{
    public class ODDBDataService
    {
        public bool LoadDatabase(string path, out ODDatabase database)
        {
            database = null;
            if (!File.Exists(path))
            {
                Debug.LogError($"Database file not found at path: {path}");
                return false;
            }
            var json = File.ReadAllText(path);
                    
            var converter = new ODDBConverter();
            database = converter.CreateDatabase(json);
            return database != null;
        }

        public bool SaveDatabase(ODDatabase database, string path)
        {
            try
            {
                var converter = new ODDBConverter();
                var databaseDto = converter.Export(database);

                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(path, databaseDto);
                Debug.Log($"Database saved to: {path}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving database: {e.Message}");
                return false;
            }
        }
    }
} 