using System.IO;
using System.Xml.Serialization;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.DTO;
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
                return SaveFile(path, databaseDto);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving database: {e.Message}");
                return false;
            }
        }
        
        public bool SaveFile(string path, string content)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(path, content);
                Debug.Log($"File saved to: {path}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving file: {e.Message}");
                return false;
            }
        }
        
        public bool LoadFile(string path, out string content)
        {
            content = null;
            if (!File.Exists(path))
            {
                Debug.LogError($"File not found at path: {path}");
                return false;
            }
            try
            {
                content = File.ReadAllText(path);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error reading file: {e.Message}");
                return false;
            }
        }
    }
} 