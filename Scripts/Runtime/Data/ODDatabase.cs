using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Plugins.ODDB.Scripts.Runtime.Data.Repositories;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDatabase : IODDatabase
    {
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
        }
        public ODDBID CreateKey()
        {
            var newid = new ODDBID();
            while (IsKeyExists(newid))
                newid = new ODDBID();
            return newid;
        }
        
        private bool IsKeyExists(ODDBID key)
        {
            foreach (var repo in _repositories.Values)
            {
                if (repo.Read(key) != null)
                    return true;
            }
            return false;
        }

        public bool TrySerialize(out string data)
        {
            if (Tables.TrySerialize(out var tableRepoData) && Views.TrySerialize(out var viewRepoData))
            {
                var databaseDto = new ODDatabaseDTO(tableRepoData, viewRepoData);
                data = JsonUtility.ToJson(databaseDto);
                Debug.Log(data);
                return true;
            }
            data = null;
            return false;
        }

        public bool TryDeserialize(string data)
        {
            var databaseDto = JsonUtility.FromJson<ODDatabaseDTO>(data);
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
    }
}