using System;
using System.Linq;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.DTO;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils;

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
    }
}