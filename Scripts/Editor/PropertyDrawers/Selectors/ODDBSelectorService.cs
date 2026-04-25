using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Service to provide options and names for ODDB ID selectors
    /// </summary>
    public class ODDBSelectorService
    {
        private const long CACHE_DURATION_MS = 30000; // 30 seconds
        private static long _lastCacheTime = 0;
        private void Refresh()
        {
            var elapsed = DateTime.Now.Ticks - _lastCacheTime;
            if (elapsed < CACHE_DURATION_MS)
                return;
            _lastCacheTime = DateTime.Now.Ticks;
            if (ODDBPort.IsInitialized == false)
                ODDBPort.Initialize();
        }
        
        public List<string> GetTypeEntities(Type type)
        {
            Refresh();
            return ODDBPort
                .GetEntities(type)
                .Select(entity => entity.ID)
                .ToList();
        }

        public List<ODDBIDSelectorOption> GetOptions(params Type[] filterTypes)
        {
            Refresh();

            if (filterTypes == null || filterTypes.Length == 0)
                return new List<ODDBIDSelectorOption>();

            return filterTypes
                .Where(type => type != null)
                .SelectMany(type => ODDBPort.GetEntities(type))
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
            return ODDBPort.TryGetEntity<ODDBEntity>(id, out _);
        }
    }
}
