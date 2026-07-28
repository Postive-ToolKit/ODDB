using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
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
        private static readonly long CACHE_DURATION_TICKS =
            TimeSpan.TicksPerMillisecond * CACHE_DURATION_MS;
        private static readonly Dictionary<string, List<ODDBIDSelectorOption>> _optionsCache = new();
        private static readonly Dictionary<string, bool> _validityCache = new();
        private static IODDBEditorUseCase _subscribedUseCase;
        private static ODDatabase _cachedDatabase;
        private static long _cacheExpiresAtTicks;

        private static ODDatabase PrepareDatabase()
        {
            try
            {
                var useCase = ODDBEditorRuntime.UseCase;
                if (!ReferenceEquals(_subscribedUseCase, useCase))
                {
                    if (_subscribedUseCase != null)
                        _subscribedUseCase.OnViewChanged -= Invalidate;
                    _subscribedUseCase = useCase;
                    if (_subscribedUseCase != null)
                        _subscribedUseCase.OnViewChanged += Invalidate;
                    Invalidate();
                }

                var db = useCase?.DataBase as ODDatabase;
                var now = DateTime.UtcNow.Ticks;
                if (!ReferenceEquals(_cachedDatabase, db) || now >= _cacheExpiresAtTicks)
                {
                    _cachedDatabase = db;
                    _optionsCache.Clear();
                    _validityCache.Clear();
                    _cacheExpiresAtTicks = now + CACHE_DURATION_TICKS;
                }

                if (db != null && !db.IsPorted)
                {
                    try { db.PortData(); }
                    catch (Exception e)
                    {
                        TeamODD.ODDB.Runtime.ODDB.Logger.Warn(
                            $"[ODDBSelectorService] PortData failed: {e.Message}");
                    }
                }
                return db;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void Invalidate(string _ = null)
        {
            _optionsCache.Clear();
            _validityCache.Clear();
            _cacheExpiresAtTicks = 0;
        }

        public List<string> GetTypeEntities(Type type)
        {
            if (type == null)
                return new List<string>();
            return GetOptions(type)
                .Select(option => option.ID)
                .ToList();
        }

        public List<ODDBIDSelectorOption> GetOptions(params Type[] filterTypes)
        {
            var types = (filterTypes ?? Array.Empty<Type>())
                .Where(type => type != null)
                .Distinct()
                .OrderBy(type => type.AssemblyQualifiedName)
                .ToArray();
            if (types.Length == 0)
                types = new[] { typeof(ODDBEntity) };

            var db = PrepareDatabase();
            if (db == null)
                return new List<ODDBIDSelectorOption>();

            var cacheKey = string.Join("|", types.Select(type => type.AssemblyQualifiedName));
            if (_optionsCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var options = types
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
            _optionsCache[cacheKey] = options;
            return options;
        }

        /// <summary>
        /// Check if the given ID is valid in the database
        /// </summary>
        /// <param name="id"> check this id </param>
        /// <returns> true if valid, otherwise false </returns>
        public bool IsValidID(string id, params Type[] filterTypes)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            var db = PrepareDatabase();
            if (db == null)
                return false;
            var types = (filterTypes ?? Array.Empty<Type>())
                .Where(type => type != null)
                .Distinct()
                .OrderBy(type => type.AssemblyQualifiedName)
                .ToArray();
            var cacheKey = id + "|" + string.Join("|", types.Select(type => type.AssemblyQualifiedName));
            if (_validityCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var isValid = db.TryGetEntity<ODDBEntity>(id, out var entity)
                          && IsAllowedEntityType(entity.GetType(), types);
            _validityCache[cacheKey] = isValid;
            return isValid;
        }

        internal static bool IsAllowedEntityType(Type entityType, IReadOnlyCollection<Type> filterTypes)
        {
            if (entityType == null) return false;
            return filterTypes == null
                   || filterTypes.Count == 0
                   || filterTypes.Any(type => type != null && type.IsAssignableFrom(entityType));
        }
    }
}
