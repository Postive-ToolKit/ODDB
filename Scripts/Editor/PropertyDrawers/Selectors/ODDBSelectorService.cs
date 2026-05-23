using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Settings;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    public sealed class ODDBIDSelectorOption
    {
        public Type EntityType { get; }
        public string ID { get; }
        public string DisplayName { get; }

        public ODDBIDSelectorOption(Type entityType, string id, string displayName)
        {
            EntityType = entityType;
            ID = id;
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// Service to provide options and names for ODDB ID selectors.
    /// Resolves entities from the Editor's <see cref="ODDBEditorRuntime"/> database
    /// so selectors work in the Inspector without a runtime <c>ODDBPort</c> facade.
    /// </summary>
    public class ODDBSelectorService
    {
        private const long CACHE_DURATION_MS = 30000; // 30 seconds
        private static long _lastCacheTime = 0;

        private static ODDatabase ResolveDatabase()
        {
            try
            {
                return ODDBEditorRuntime.UseCase?.DataBase as ODDatabase;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void Refresh()
        {
            var elapsed = DateTime.Now.Ticks - _lastCacheTime;
            if (elapsed < CACHE_DURATION_MS)
                return;
            _lastCacheTime = DateTime.Now.Ticks;

            var db = ResolveDatabase();
            if (db != null && !db.IsPorted)
            {
                try { db.PortData(); }
                catch (Exception e) { TeamODD.ODDB.Runtime.ODDB.Logger.Warn($"[ODDBSelectorService] PortData failed: {e.Message}"); }
            }
        }

        public List<string> GetTypeEntities(Type type)
        {
            Refresh();
            var db = ResolveDatabase();
            if (db == null)
                return new List<string>();
            return db.GetEntities(type)
                .Select(entity => entity.ID)
                .ToList();
        }

        public List<ODDBIDSelectorOption> GetOptions(params Type[] filterTypes)
        {
            Refresh();

            if (filterTypes == null || filterTypes.Length == 0)
                return new List<ODDBIDSelectorOption>();

            var db = ResolveDatabase();
            if (db == null)
                return new List<ODDBIDSelectorOption>();

            return filterTypes
                .Where(type => type != null)
                .SelectMany(type => db.GetEntities(type))
                .GroupBy(entity => entity.ID)
                .Select(group => group.First())
                .OrderBy(entity => entity.GetType().Name)
                .ThenBy(entity => entity.ID)
                .Select(entity => new ODDBIDSelectorOption(
                    entity.GetType(),
                    entity.ID,
                    entity.ID))
                .ToList();
        }

        /// <summary>
        /// Check if the given ID is valid in the database
        /// </summary>
        /// <param name="id"> check this id </param>
        /// <returns> true if valid, otherwise false </returns>
        public bool IsValidID(string id)
        {
            Refresh();
            var db = ResolveDatabase();
            if (db == null)
                return false;
            return db.TryGetEntity<ODDBEntity>(id, out _);
        }
    }
}
