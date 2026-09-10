using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.DTO;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Types;
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

            viewRepo.OnDataChanged += (id) =>
            {
                OnDataChanged?.Invoke(id);
            };
            viewRepo.OnDataRemoved += (id) =>
            {
                OnDataRemoved?.Invoke(id);
            };
            tableRepo.OnDataChanged += (id) =>
            {
                OnDataChanged?.Invoke(id);
            };
            tableRepo.OnDataRemoved += (id) =>
            {
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
        /// Loads an ODDatabase from a compressed binary file. Throws on any failure
        /// (missing file, read/gzip/json failure, broken DTO). Callers that need a
        /// fallback should use <see cref="TryLoad"/> or <see cref="CreateEmpty"/>.
        /// </summary>
        public static ODDatabase Load(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("filePath required", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("ODDB file not found", filePath);

            var bytes = File.ReadAllBytes(filePath);
            var converter = new ODDBConverter();
            if (!converter.TryImportDTO(bytes, out var dto, out var stage, out var reason))
            {
                ODDBConverter.OnDatabaseCreated.Clear();
                throw new ODDBLoadException(stage, reason);
            }
            var db = new ODDatabase();
            db.FromDTO(dto);
            FireOnDatabaseCreated(db);
            return db;
        }

        /// <summary>
        /// Safe-load. Returns false on any failure with a populated <see cref="ODDBLoadReport"/>
        /// describing the stage of failure. On EmptyRestoredOnNonEmptyDto and UnmappedFieldType
        /// failures, <paramref name="database"/> is populated for read-only inspection but
        /// <see cref="ODDBLoadReport.IsSafeToSave"/> is false; the caller MUST NOT save it back.
        /// </summary>
        public static bool TryLoad(string filePath, out ODDatabase database, out ODDBLoadReport report)
        {
            database = null;
            if (string.IsNullOrEmpty(filePath))
            {
                report = ODDBLoadReport.Failure(ODDBLoadFailureStage.FileMissing, "filePath null/empty",
                    0, 0, 0, 0, 0, 0, "ambiguous");
                return false;
            }
            if (!File.Exists(filePath))
            {
                report = ODDBLoadReport.Failure(ODDBLoadFailureStage.FileMissing, "file not found",
                    0, 0, 0, 0, 0, 0, "ambiguous");
                return false;
            }

            long size;
            byte[] bytes;
            try
            {
                size = new FileInfo(filePath).Length;
                bytes = File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                report = ODDBLoadReport.Failure(ODDBLoadFailureStage.Read, ex.Message,
                    0, 0, 0, 0, 0, 0, "ambiguous");
                return false;
            }

            return TryLoadBytes(bytes, out database, out report);
        }

        private static readonly object SafeLoadLock = new object();

        /// <summary>
        /// Safe-load a compressed payload from an engine asset, package or stream.
        /// Applies exactly the same validation and hydration as TryLoad(path).
        /// </summary>
        public static bool TryLoadBytes(byte[] bytes, out ODDatabase database, out ODDBLoadReport report)
        {
            lock (SafeLoadLock)
            {
                try
                {
                    _ = TypeRegistry.All;
                    return TryRestoreBytes(bytes, out database, out report);
                }
                catch (Exception exception)
                {
                    database = null;
                    report = ODDBLoadReport.Failure(ODDBLoadFailureStage.Restore, exception.Message,
                        bytes?.LongLength ?? 0, 0, 0, 0, 0, 0, "ambiguous");
                    return false;
                }
                finally
                {
                    ODDBConverter.OnDatabaseCreated.Clear();
                }
            }
        }

        private static bool TryRestoreBytes(byte[] bytes, out ODDatabase database, out ODDBLoadReport report)
        {
            database = null;
            var size = bytes?.LongLength ?? 0;
            var converter = new ODDBConverter();
            if (!converter.TryImportDTO(bytes, out var dto, out var stage, out var reason))
            {
                report = ODDBLoadReport.Failure(stage, reason, size, 0, 0, 0, 0, 0, "ambiguous");
                return false;
            }

            var dtoTableCount = dto.TableRepoData?.Count ?? 0;
            var dtoViewCount = dto.ViewRepoData?.Count ?? 0;
            var sourceFormat = ProbeSourceFormat(dto);

            // Per Pre-mortem #2 (resolution b): empty-DTO-on-existing-file is ALWAYS fatal,
            // even when SourceFormatVersion == "ambiguous". The legitimate-empty case is
            // recovered via the migration script's --allow-empty override (exit code 10),
            // NOT via in-editor save. Keeps Principle 3 (Save refuses, no warnings) exception-free.
            if (size > 0 && dtoTableCount == 0 && dtoViewCount == 0)
            {
                ODDBConverter.OnDatabaseCreated.Clear();
                report = ODDBLoadReport.Failure(ODDBLoadFailureStage.EmptyDtoOnExistingFile,
                    "DTO has zero tables and zero views on a non-empty file",
                    size, 0, 0, 0, 0, 0, sourceFormat);
                return false;
            }

            var db = new ODDatabase();
            db.FromDTO(dto);
            var restoredT = db.Tables.Count;
            var restoredV = db.Views.Count;

            if ((dtoTableCount + dtoViewCount > 0) && (restoredT + restoredV == 0))
            {
                ODDBConverter.OnDatabaseCreated.Clear();
                report = ODDBLoadReport.Failure(ODDBLoadFailureStage.EmptyRestoredOnNonEmptyDto,
                    "DTO had data but FromDTO restored zero entities",
                    size, dtoTableCount, dtoViewCount, 0, 0, 0, sourceFormat);
                database = db;
                return false;
            }

            // CRITICAL: drain OnDatabaseCreated to hydrate Table rows + view parent-bindings.
            // Table.FromDTO caches row data in _cachedData and queues OnDatabaseInitialize;
            // without this call rows stay unhydrated even though the file has data.
            FireOnDatabaseCreated(db);

            // A v2 file carries an explicit string key for every field. A key that this
            // process does not know belongs to a consumer extension (for example a
            // Unity-only serializer), not to a corrupt database. Cell falls back to a
            // string serializer, so the original key and serialized value can be
            // round-tripped without pretending that the type is materializable here.
            var unmapped = CountUnmappedFieldTypes(db);
            report = ODDBLoadReport.Success(size, dtoTableCount, dtoViewCount, restoredT, restoredV,
                sourceFormat, unmapped);
            database = db;
            return true;
        }

        /// <summary>
        /// Explicit new-DB intent. No load happens. Used by the fresh-install branch.
        /// </summary>
        public static ODDatabase CreateEmpty() => new ODDatabase();

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

        /// <summary>
        /// Fires the <see cref="ODDBConverter.OnDatabaseCreated"/> callback list against
        /// the freshly-built database and clears the list. Table.FromDTO registers a
        /// row-hydration callback there; without this drain, <c>Table.Rows</c> stays
        /// empty even when the source file has data. Mirrors the legacy
        /// <c>ODDBConverter.Import</c> contract for success paths.
        /// </summary>
        private static void FireOnDatabaseCreated(ODDatabase db)
        {
            ODDBConverter.OnDatabaseCreated.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            foreach (var createEvent in ODDBConverter.OnDatabaseCreated)
                createEvent.OnEvent?.Invoke(db);
            ODDBConverter.OnDatabaseCreated.Clear();
        }

        private static string ProbeSourceFormat(DatabaseDTO dto)
        {
            // v2 serializes Field.Type with a non-empty "_typeKey" property.
            // v1 dumps either lack the property or have null/empty TypeKey values.
            // ambiguous = empty DTO or mixed evidence.
            if (dto == null) return "ambiguous";
            var tableCount = dto.TableRepoData?.Count ?? 0;
            var viewCount = dto.ViewRepoData?.Count ?? 0;
            if (tableCount == 0 && viewCount == 0) return "ambiguous";

            int withKey = 0, total = 0;
            void TallyFields(List<Field> fields)
            {
                if (fields == null) return;
                foreach (var f in fields)
                {
                    total++;
                    if (f != null && f.Type != null && !string.IsNullOrEmpty(f.Type.TypeKey))
                        withKey++;
                }
            }
            if (dto.TableRepoData != null)
                foreach (var t in dto.TableRepoData)
                    TallyFields(t?.TableMetas);
            if (dto.ViewRepoData != null)
                foreach (var v in dto.ViewRepoData)
                    TallyFields(v?.TableMetas);

            if (total == 0) return "ambiguous";
            if (withKey == total) return "v2";
            if (withKey == 0) return "v1";
            return "ambiguous";
        }

        private static int CountUnmappedFieldTypes(ODDatabase db)
        {
            int unmapped = 0;
            void Walk(IRepository<IView> repo)
            {
                if (repo == null) return;
                foreach (var view in repo.GetAll())
                {
                    if (view == null) continue;
                    var fields = view.ScopedFields;
                    if (fields == null) continue;
                    foreach (var field in fields)
                    {
                        if (field?.Type == null) continue;
                        var key = field.Type.TypeKey;
                        if (string.IsNullOrEmpty(key)) continue;
                        if (TypeRegistry.GetDescriptor(key) == null)
                            unmapped++;
                    }
                }
            }
            Walk(db.Tables);
            Walk(db.Views);
            return unmapped;
        }

        #endregion

        #region Instance Entity API

        private Dictionary<string, ODDBEntity> _entityCache;
        private Dictionary<Type, Dictionary<string, ODDBEntity>> _entityTypeCache;
        private readonly List<Action> _onDataPortedCallbacks = new List<Action>();
        private bool _isPorted;
        private ODDBPortOperation _activePortOperation;

        internal sealed class PortWorkItem
        {
            public Type TargetType;
            public List<Field> Fields;
            public List<Row> Rows;
        }

        /// <summary>
        /// Whether PortData has materialized entities from rows on this instance.
        /// </summary>
        public bool IsPorted => _isPorted;

        /// <summary>
        /// Instantiates strongly-typed entities from each Table's rows using their BindType
        /// and caches them for GetEntity/GetEntities lookups.
        /// </summary>
        public void PortData()
        {
            using var operation = BeginPortData();
            operation.Complete();
        }

        /// <summary>
        /// Starts generation-safe, incremental entity materialization.
        /// Call <see cref="ODDBPortOperation.Step"/> from the engine main loop and
        /// return control between batches. The operation must be completed or
        /// cancelled before starting another operation on this database.
        /// </summary>
        public ODDBPortOperation BeginPortData()
        {
            if (_isPorted)
                return ODDBPortOperation.Completed(this);
            if (_activePortOperation != null && !_activePortOperation.IsCompleted)
                throw new InvalidOperationException("Entity materialization is already in progress.");

            _entityCache = new Dictionary<string, ODDBEntity>();
            _entityTypeCache = new Dictionary<Type, Dictionary<string, ODDBEntity>>();
            _onDataPortedCallbacks.Clear();

            var workItems = new List<PortWorkItem>();
            var totalRows = 0;

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

                var rows = table.Rows;
                workItems.Add(new PortWorkItem
                {
                    TargetType = targetType,
                    Fields = table.TotalFields,
                    Rows = rows,
                });
                totalRows += rows.Count;
            }

            _activePortOperation = new ODDBPortOperation(this, workItems, totalRows);
            return _activePortOperation;
        }

        internal void MaterializePortRow(Type targetType, List<Field> fields, Row row)
        {
            var entity = Activator.CreateInstance(targetType) as ODDBEntity;
            if (entity == null)
            {
                ODDB.Logger.Error($"Failed to create instance of {targetType}");
                return;
            }

            entity.Import(this, fields, row);
            _entityCache[row.ID] = entity;
            _entityTypeCache[targetType][row.ID] = entity;
        }

        internal void CompletePortData(ODDBPortOperation operation)
        {
            if (!ReferenceEquals(_activePortOperation, operation))
                throw new InvalidOperationException("Entity materialization operation is no longer active.");

            _isPorted = true;
            _activePortOperation = null;

            foreach (var cb in _onDataPortedCallbacks)
                cb?.Invoke();
            _onDataPortedCallbacks.Clear();
        }

        internal void AbortPortData(ODDBPortOperation operation)
        {
            if (_activePortOperation != null && !ReferenceEquals(_activePortOperation, operation))
                return;

            _activePortOperation = null;
            _entityCache = null;
            _entityTypeCache = null;
            _onDataPortedCallbacks.Clear();
            _isPorted = false;
        }

        private void EnsurePorted()
        {
            if (!_isPorted)
                PortData();
        }

        public T GetEntity<T>(string id)
        {
            return TryGetEntity<T>(id, out var entity) ? entity : default;
        }

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

        public IEnumerable<T> GetEntities<T>()
        {
            return GetEntities(typeof(T)).OfType<T>();
        }

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

        public IEnumerable<string> GetLiveEntityIds()
        {
            return _entityCache != null ? _entityCache.Keys : Enumerable.Empty<string>();
        }

        #endregion
    }
}
