using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.DTO;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Runtime
{
    public class ODDatabase : IODDatabase, IDBDataObserver
    {
        public event Action<ODDBID> OnDataChanged;
        public event Action<ODDBID> OnDataRemoved;
        private readonly Type _tableType = typeof(Table);
        private readonly Type _viewType = typeof(View);
        private readonly Dictionary<Type, IRepository<IView>> _repositories = new();
        public IRepository<IView> Tables => _repositories[_tableType];
        public IRepository<IView> Views => _repositories[_viewType];
        public int Count => Tables.Count + Views.Count;

        public ODDatabase()
        {
            var tableRepo = new ViewRepository<Table>();
            var viewRepo = new ViewRepository<View>();
            viewRepo.KeyProvider = this;
            tableRepo.KeyProvider = this;
            _repositories.Add(typeof(Table), tableRepo);
            _repositories.Add(typeof(View), viewRepo);
            
            viewRepo.OnDataChanged += (id) => {
                OnDataChanged?.Invoke(id);
            };
            viewRepo.OnDataRemoved += (id) => {
                OnDataRemoved?.Invoke(id);
            };
            tableRepo.OnDataChanged += (id) => {
                OnDataChanged?.Invoke(id);
            };
            tableRepo.OnDataRemoved += (id) => {
                OnDataRemoved?.Invoke(id);
            };
        }
        public ODDBID CreateID()
        {
            var newid = new ODDBID();
            while (IsKeyExists(newid))
                newid = new ODDBID();
            return newid;
        }
        
        private bool IsKeyExists(ODDBID id)
        {
            foreach (var repo in _repositories.Values)
            {
                if (repo.Read(id) != null)
                    return true;
            }
            return false;
        }

        public IView GetView(ODDBID id)
        {
            foreach (var repo in _repositories.Values)
            {
                var view = repo.Read(id);
                if (view != null)
                    return view;
            }
            return null;
        }
        
        public IReadOnlyList<IView> GetAll()
        {
            var allViews = new List<IView>();
            foreach (var repo in _repositories.Values)
            {
                allViews.AddRange(repo.GetAll());
            }

            return allViews;
        }

        public void NotifyDataChanged(ODDBID id)
        {
            OnDataChanged?.Invoke(id);
        }
        
        public DatabaseDTO ToDTO()
        {
            var tables = Tables.GetAll();
            var views = Views.GetAll();
            
            var tableDtos = tables.Select(t => t.ToDTO() as TableDTO).ToList();
            var viewDtos = views.Select(v => v.ToDTO() as ViewDTO).ToList();
            
            return new DatabaseDTO(tableDtos, viewDtos);
        }

        public void FromDTO(DatabaseDTO dto)
        {
            if (dto.TableRepoData != null)
            {
                foreach (var tableDto in dto.TableRepoData)
                {
                    var table = Tables.Create(new ODDBID(tableDto.ID));
                    table.FromDTO(tableDto);
                }
            }

            if (dto.ViewRepoData != null)
            {
                foreach (var viewDto in dto.ViewRepoData)
                {
                    var view = Views.Create(new ODDBID(viewDto.ID));
                    view.FromDTO(viewDto);
                }
            }
        }

        public void Clear()
        {
            Tables.Clear();
            Views.Clear();
            OnDataChanged = null;
            OnDataRemoved = null;
        }

        /// <summary>
        /// Clears only the row data from all tables to save memory at runtime.
        /// Use this after PortData is completed.
        /// </summary>
        public void ClearTableData()
        {
            foreach (var view in Tables.GetAll())
            {
                if (view is Table table)
                {
                    table.Clear();
                }
            }
        }

        #region Load / Save

        /// <summary>
        /// Loads an ODDatabase instance from a compressed binary file at the given path.
        /// Returns an empty database if path is null/empty or the file does not exist.
        /// </summary>
        public static ODDatabase Load(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return new ODDatabase();
            if (!File.Exists(filePath))
            {
                ODDB.Logger.Info($"DB file not found at {filePath} — returning empty database");
                return new ODDatabase();
            }
            var bytes = File.ReadAllBytes(filePath);
            var converter = new ODDBConverter();
            return converter.Import(bytes) ?? new ODDatabase();
        }

        /// <summary>
        /// Saves this database to a compressed binary file at the given path.
        /// Creates parent directories as needed.
        /// </summary>
        public void Save(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("filePath required", nameof(filePath));
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var converter = new ODDBConverter();
            var bytes = converter.Export(this);
            File.WriteAllBytes(filePath, bytes);
        }

        #endregion

        #region Instance Entity API

        private Dictionary<string, ODDBEntity> _entityCache;
        private Dictionary<Type, Dictionary<string, ODDBEntity>> _entityTypeCache;
        private readonly List<Action> _onDataPortedCallbacks = new List<Action>();
        private bool _isPorted;

        /// <summary>
        /// Whether PortData has materialized entities from rows on this instance.
        /// </summary>
        public bool IsPorted => _isPorted;

        /// <summary>
        /// Instantiates strongly-typed entities from each Table's rows using their BindType
        /// and caches them for GetEntity/GetEntities lookups. Idempotent — subsequent calls
        /// rebuild the cache.
        /// </summary>
        public void PortData()
        {
            _entityCache = new Dictionary<string, ODDBEntity>();
            _entityTypeCache = new Dictionary<Type, Dictionary<string, ODDBEntity>>();

            foreach (var view in Tables.GetAll())
            {
                var targetType = view.BindType;
                if (targetType == null)
                {
                    ODDB.Logger.Warn(
                        $"BindType is null for table {view.Name} with key {view.ID}, table will be excluded.");
                    continue;
                }

                if (view is not Table table)
                    continue;

                if (!_entityTypeCache.ContainsKey(targetType))
                    _entityTypeCache[targetType] = new Dictionary<string, ODDBEntity>();

                foreach (var row in table.Rows)
                {
                    var entity = Activator.CreateInstance(targetType) as ODDBEntity;
                    if (entity == null)
                    {
                        ODDB.Logger.Error($"Failed to create instance of {targetType}");
                        continue;
                    }

                    entity.Import(table.TotalFields, row);
                    _entityCache[row.ID] = entity;
                    _entityTypeCache[targetType][row.ID] = entity;
                }
            }

            _isPorted = true;

            foreach (var cb in _onDataPortedCallbacks)
                cb?.Invoke();
            _onDataPortedCallbacks.Clear();
        }

        private void EnsurePorted()
        {
            if (!_isPorted)
                PortData();
        }

        /// <summary>
        /// Get entity of type T by ID, or default if not found / type mismatch.
        /// </summary>
        public T GetEntity<T>(string id)
        {
            return TryGetEntity<T>(id, out var entity) ? entity : default;
        }

        /// <summary>
        /// Try to get entity of type T by ID.
        /// </summary>
        public bool TryGetEntity<T>(string id, out T result)
        {
            result = default;
            if (string.IsNullOrEmpty(id))
                return false;

            EnsurePorted();

            if (!_entityCache.TryGetValue(id, out var entity))
                return false;

            if (entity is T typedEntity)
            {
                result = typedEntity;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get all entities of type T (or assignable from T).
        /// </summary>
        public IEnumerable<T> GetEntities<T>()
        {
            return GetEntities(typeof(T)).OfType<T>();
        }

        /// <summary>
        /// Get all entities whose runtime type is assignable to the specified type.
        /// </summary>
        public IEnumerable<ODDBEntity> GetEntities(Type type)
        {
            if (type == null)
                return Enumerable.Empty<ODDBEntity>();

            EnsurePorted();

            var result = new Dictionary<string, ODDBEntity>();
            foreach (var kvp in _entityTypeCache.ToList())
            {
                if (!type.IsAssignableFrom(kvp.Key))
                    continue;
                foreach (var entity in kvp.Value.Values)
                    result[entity.ID] = entity;
            }

            _entityTypeCache[type] = result;
            return result.Values;
        }

        /// <summary>
        /// Registers a callback to invoke after PortData completes. If already ported,
        /// the callback fires immediately.
        /// </summary>
        public void RegisterOnDataPorted(Action callback)
        {
            if (callback == null)
            {
                ODDB.Logger.Error("Cannot register a null callback.");
                return;
            }

            if (_isPorted)
            {
                callback.Invoke();
                return;
            }
            _onDataPortedCallbacks.Add(callback);
        }

        /// <summary>
        /// Enumerates the IDs of entities currently live in this instance's entity cache.
        /// </summary>
        public IEnumerable<string> GetLiveEntityIds()
        {
            return _entityCache != null ? _entityCache.Keys : Enumerable.Empty<string>();
        }

        #endregion
    }
}