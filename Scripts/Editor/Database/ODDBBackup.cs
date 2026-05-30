using System;
using System.IO;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Window
{
    /// <summary>
    /// Pre-save / pre-import backup helper shared by <see cref="ODDBEditorUseCase"/> and
    /// <c>ODDBDataService.SaveDatabase</c>. Best-effort: never blocks a save on backup failure.
    /// </summary>
    public static class ODDBBackup
    {
        public static void CreatePreSaveBackup(string fullPath, int keep)
        {
            try
            {
                if (string.IsNullOrEmpty(fullPath)) return;
                if (!File.Exists(fullPath)) return;   // fresh install — no source file to back up
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var backupPath = $"{fullPath}.pre-save-{timestamp}.bak";
                File.Copy(fullPath, backupPath, overwrite: true);
                RotateBackups(fullPath, keep, ".pre-save-*.bak");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ODDB] pre-save backup failed: {e.Message}");
                // Best-effort: do NOT throw — backup failure must not block a valid save.
            }
        }

        /// <summary>
        /// Generic rotation: keeps the newest <paramref name="keep"/> backup files matching
        /// <c>{basename}{searchPatternSuffix}</c> in the same directory, deletes the rest.
        /// Pre-import uses <c>".preimport-*.bak"</c>; pre-save uses <c>".pre-save-*.bak"</c>.
        /// </summary>
        public static void RotateBackups(string originalFullPath, int keep, string searchPatternSuffix)
        {
            if (string.IsNullOrEmpty(originalFullPath)) return;
            var directory = Path.GetDirectoryName(originalFullPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;
            var baseName = Path.GetFileName(originalFullPath);
            var pattern = baseName + searchPatternSuffix;
            string[] backups;
            try { backups = Directory.GetFiles(directory, pattern); }
            catch { return; }
            if (backups.Length <= keep) return;
            Array.Sort(backups, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
            for (var i = keep; i < backups.Length; i++)
            {
                try { File.Delete(backups[i]); } catch { /* best-effort rotation */ }
            }
        }
    }
}
