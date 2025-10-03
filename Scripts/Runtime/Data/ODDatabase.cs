using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;
using UnityEngine;

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
        
        public ODDatabaseDTO ToDTO()
        {
            var tables = Tables.GetAll();
            var views = Views.GetAll();
            
            var tableDtos = tables.Select(t => t.ToDTO() as ODDBTableDTO).ToList();
            var viewDtos = views.Select(v => v.ToDTO() as ODDBViewDTO).ToList();
            
            return new ODDatabaseDTO(tableDtos, viewDtos);
        }

        public void FromDTO(ODDatabaseDTO dto)
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
    }
}