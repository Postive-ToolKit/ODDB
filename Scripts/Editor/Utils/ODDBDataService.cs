using System.IO;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Utils
{
    public class ODDBDataService
    {
        private const int PreSaveBackupKeep = 3;

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
            // Structural empty-DB-on-existing-file refusal. Closes the leak where
            // ODDBSheetConverter.SaveDatabaseToFile's non-useCase fallback branch
            // bypasses the use-case CanSave gate. Principle 3: Save refuses, no warnings.
            if (database != null && File.Exists(path) &&
                database.Tables.Count + database.Views.Count == 0)
            {
                long fileSize = 0;
                try { fileSize = new FileInfo(path).Length; } catch { }
                Debug.LogError(
                    $"[ODDB][SAVE-REFUSED] reason=empty-db-on-existing-file " +
                    $"path={path} size={fileSize} callsite=ODDBDataService.SaveDatabase");
                return false;
            }

            // Pre-save backup at the DataService layer — closes the ODDBSheetConverter
            // fallback leak where the editor wrapper's backup is skipped.
            ODDBBackup.CreatePreSaveBackup(path, PreSaveBackupKeep);

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