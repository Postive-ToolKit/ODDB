using System.IO;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Utils
{
    public class ODDBDataService
    {
        public bool LoadDatabase(string path, out ODDatabase database)
        {
            database = null;
            if (LoadFile(path, out var binary) == false)
            {
                Debug.LogError($"Failed to load file at path: {path}");
                return false;
            }
            var converter = new ODDBConverter();
            database = converter.Import(binary);
            return database != null;
        }

        public bool SaveDatabase(ODDatabase database, string path)
        {
            try
            {
                var converter = new ODDBConverter();
                var binary = converter.Export(database);
                return SaveFile(path, binary);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving database: {e.Message}");
                return false;
            }
        }

        public bool LoadFile(string path, out byte[] content)
        {
            content = null;
            if (!File.Exists(path))
            {
                Debug.LogError($"File not found at path: {path}");
                return false;
            }
            content = File.ReadAllBytes(path);
            return true;
        }

        public bool SaveFile(string path, byte[] content)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllBytes(path, content);
                Debug.Log($"File saved to: {path}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving file: {e.Message}");
                return false;
            }
        }
    }
} 