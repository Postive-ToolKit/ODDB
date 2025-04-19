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
            var serializer = new XmlSerializer(typeof(ODDatabaseDTO));
            using var stream = new FileStream(path, FileMode.Open);
            var databaseDto = (ODDatabaseDTO)serializer.Deserialize(stream);
            Debug.Log($"Database loaded from: {path}");
                    
            var importer = new ODDBImporter();
            database = importer.CreateDatabase(databaseDto);
            return database != null;
        }

        public bool SaveDatabase(ODDatabase database, string path)
        {
            try
            {
                var exporter = new ODDBExporter();
                var databaseDto = exporter.Export(database);

                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var serializer = new XmlSerializer(typeof(ODDatabaseDTO));
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    serializer.Serialize(stream, databaseDto);
                }
                
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