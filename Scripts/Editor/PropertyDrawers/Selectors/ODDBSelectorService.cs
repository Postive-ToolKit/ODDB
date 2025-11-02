using System;
using System.Collections.Generic;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Settings;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Service to provide options and names for ODDB ID selectors
    /// </summary>
    public class ODDBSelectorService
    {
        private const string NO_ENTITY_FOUND = "No Entity Found";
        private const long CACHE_DURATION_MS = 30000; // 30 seconds
        private static long _lastCacheTime = 0;
        private Dictionary<string, List<string>> _cachedOptions = new Dictionary<string, List<string>>();
        private Dictionary<string, string> _cachedNames = new Dictionary<string, string>();
        private Dictionary<string, string> _optionTable = new Dictionary<string, string>();

        private void Refresh()
        {
            var elapsed = DateTime.Now.Ticks - _lastCacheTime;
            if (elapsed < CACHE_DURATION_MS)
                return;
            _lastCacheTime = DateTime.Now.Ticks;
            var dataService = new ODDBDataService();
            dataService.LoadDatabase(ODDBSettings.Setting.FullDBPath, out var db);
            _cachedNames.Clear();
            _cachedOptions.Clear();
            var tables = db.Tables;
            foreach (var view in tables.GetAll())
            {
                var table = view as Table;
                if (table == null)
                    continue;
                _cachedNames.Add(table.ID, table.Name);
                _cachedOptions.Add(table.ID, new List<string>());
                foreach (var record in table.Rows)
                {
                    _cachedOptions[table.ID].Add(record.ID);
                    _cachedNames[record.ID.ToString()] = record.GetName();
                    _optionTable[record.ID.ToString()] = table.ID;
                }
            }
        }
        
        public List<string> GetTableEntities(string tableID)
        {
            Refresh();
            if (_cachedOptions.TryGetValue(tableID, out var options))
                return options;
            return new List<string>();
        }
        
        /// <summary>
        /// Get the name corresponding to the given ID
        /// </summary>
        /// <param name="id"> get name by this id </param>
        /// <returns> the name if found, otherwise return the id itself </returns>
        public string GetName(string id)
        {
            Refresh();
            if (_optionTable.TryGetValue(id, out var tableID) == false)
                return NO_ENTITY_FOUND;

            if (_cachedNames.TryGetValue(id, out var name) == false)
                return NO_ENTITY_FOUND;
            return $"{_cachedNames[tableID]} - {name}";
        }
        
        public string GetPureName(string id)
        {
            Refresh();
            if (_cachedNames.TryGetValue(id, out var name) == false)
                return NO_ENTITY_FOUND;
            return name;
        }
        
        /// <summary>
        /// Check if the given ID is valid in the database
        /// </summary>
        /// <param name="id"> check this id </param>
        /// <returns> true if valid, otherwise false </returns>
        public bool IsValidID(string id)
        {
            Refresh();
            return _cachedNames.ContainsKey(id);
        }

        public IEnumerable<string> GetAllTableID()
        {
            Refresh();
            return _cachedOptions.Keys;
        }
    }
}