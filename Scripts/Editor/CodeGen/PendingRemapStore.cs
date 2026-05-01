using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TeamODD.ODDB.Editors.CodeGen
{
    /// <summary>
    /// Persistent queue of (ViewID → ClassName) entries waiting for the next
    /// successful compile so the generated type can be assigned to View.BindType.
    /// Stored under ProjectSettings/ so the queue is git-shared with teammates.
    /// </summary>
    internal static class PendingRemapStore
    {
        public const string FilePath = "ProjectSettings/ODDBGeneratorPending.json";

        public static List<PendingRemapEntry> Load()
        {
            if (!File.Exists(FilePath))
                return new List<PendingRemapEntry>();
            try
            {
                var json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return new List<PendingRemapEntry>();
                var payload = JsonUtility.FromJson<PendingRemapPayload>(json);
                return payload?.entries == null
                    ? new List<PendingRemapEntry>()
                    : new List<PendingRemapEntry>(payload.entries);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ODDB CodeGen] Failed to read pending remap store: {e.Message}. Starting fresh.");
                return new List<PendingRemapEntry>();
            }
        }

        public static void Save(IList<PendingRemapEntry> entries)
        {
            var payload = new PendingRemapPayload
            {
                entries = entries == null ? System.Array.Empty<PendingRemapEntry>() : new PendingRemapEntry[entries.Count]
            };
            for (int i = 0; i < payload.entries.Length; i++)
                payload.entries[i] = entries[i];

            var json = JsonUtility.ToJson(payload, prettyPrint: true);
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, json);
        }

        /// <summary>
        /// Adds (viewId, className) to the queue. If an entry with the same viewId already
        /// exists, it is replaced with the new className (and a fresh queuedAt timestamp).
        /// </summary>
        public static void Upsert(string viewId, string className)
        {
            var entries = Load();
            entries.RemoveAll(e => e.viewId == viewId);
            entries.Add(new PendingRemapEntry(viewId, className));
            Save(entries);
        }

        /// <summary>
        /// Batched form of <see cref="Upsert"/>: applies all pairs in one Load → mutate → Save
        /// pass. Use this when generating many views at once to avoid O(N) JSON rewrites.
        /// </summary>
        public static void UpsertMany(IEnumerable<(string viewId, string className)> items)
        {
            if (items == null) return;
            var entries = Load();
            foreach (var (viewId, className) in items)
            {
                entries.RemoveAll(e => e.viewId == viewId);
                entries.Add(new PendingRemapEntry(viewId, className));
            }
            Save(entries);
        }
    }
}
