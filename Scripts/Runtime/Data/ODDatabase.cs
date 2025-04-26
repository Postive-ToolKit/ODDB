using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDatabase : IODDatabase, IODDBDataObserver
    {
        public event Action<ODDBID> OnDataChanged;
        public event Action<ODDBID> OnDataRemoved;
        private readonly Type _tableType = typeof(ODDBTable);
        private readonly Type _viewType = typeof(ODDBView);
        private readonly Dictionary<Type, IODDBRepository<IODDBView>> _repositories = new();
        public IODDBRepository<IODDBView> Tables => _repositories[_tableType];
        public IODDBRepository<IODDBView> Views => _repositories[_viewType];
        public int Count => Tables.Count + Views.Count;

        public ODDatabase()
        {
            var tableRepo = new ODDBViewRepository<ODDBTable>();
            var viewRepo = new ODDBViewRepository<ODDBView>();
            viewRepo.KeyProvider = this;
            tableRepo.KeyProvider = this;
            _repositories.Add(typeof(ODDBTable), tableRepo);
            _repositories.Add(typeof(ODDBView), viewRepo);
            
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

        public bool TrySerialize(out string data)
        {
            if (Tables.TrySerialize(out var tableRepoData) && Views.TrySerialize(out var viewRepoData))
            {
                var databaseDto = new ODDatabaseDTO(tableRepoData, viewRepoData);
                data = JsonConvert.SerializeObject(databaseDto, Formatting.Indented);
                return true;
            }
            data = null;
            return false;
        }

        public bool TryDeserialize(string data)
        {
            var databaseDto = JsonConvert.DeserializeObject<ODDatabaseDTO>(data);
            Tables.TryDeserialize(databaseDto.TableRepoData);
            Views.TryDeserialize(databaseDto.ViewRepoData);
            return true;
        }

        public IODDBView GetView(ODDBID id)
        {
            foreach (var repo in _repositories.Values)
            {
                var view = repo.Read(id);
                if (view != null)
                    return view;
            }
            return null;
        }
        
        public IReadOnlyList<IODDBView> GetAll()
        {
            var allViews = new List<IODDBView>();
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
    }
}