using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Data.DTO.Builders;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Utils;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDBView : IODDBView
    {
        private const string DEFAULT_NAME = "Default Name";
        public ODDBID Key { get; set; }
        public string Name { get; set; }
        public Type BindType { get; set; }

        public IODDBView ParentView
        {
            get => _parentView;
            set
            {
                _parentView = value;
                if(_parentView == null)
                    return;
                if(BindType == null)
                {
                    BindType = _parentView.BindType;
                    return;
                }
                if(_parentView.BindType == null)
                    return;
                if (_parentView.BindType == BindType || BindType.IsSubclassOf(_parentView.BindType))
                    return;
                BindType = _parentView.BindType;

            }
        }
        protected ODDBID _parentViewKey;

        private IODDBView _parentView;
        
        public List<ODDBField> TotalFields
        {
            get
            {
                if (ParentView == null)
                    return _tableMetas;
                var parentTableMetas = ParentView.TotalFields;
                var tableMetas = new List<ODDBField>();
                tableMetas.AddRange(parentTableMetas);
                tableMetas.AddRange(_tableMetas);
                return tableMetas;
            }
        }
        public List<ODDBField> ScopedTableMetas => _tableMetas;
        private readonly List<ODDBField> _tableMetas = new();
        
        public ODDBView()
        {
            Key = new ODDBID();
            Name = DEFAULT_NAME;
        }
        public ODDBView(IEnumerable<ODDBField> tableMetas = null)
        {
            if (tableMetas == null)
                return;
            _tableMetas.AddRange(tableMetas);
        }
        
        public void AddField(ODDBField field)
        {
            _tableMetas.Add(field);
            OnAddTableMeta(field);
        }
        
        public void RemoveField(int index)
        {
            if (!IsScopedMeta(index)) {
                Debug.LogError($"Index {index} is out of range for this view.");
                return;
            }
            _tableMetas.RemoveAt(ConvertToScopedIndex(index));
            OnRemoveField(index);
        }
        
        public void SwapTableMeta(int indexA, int indexB)
        {
            if (!IsScopedMeta(indexA) || !IsScopedMeta(indexB))
            {
                Debug.LogError($"Index {indexA} or {indexB} is out of range for this view.");
                return;
            }
            var scopedIndexA = ConvertToScopedIndex(indexA);
            var scopedIndexB = ConvertToScopedIndex(indexB);
            (_tableMetas[scopedIndexA], _tableMetas[scopedIndexB]) = (_tableMetas[scopedIndexB], _tableMetas[scopedIndexA]);
            OnSwapTableMeta(indexA, indexB);
        }

        public bool IsScopedMeta(int index)
        {
            if (ParentView == null)
            {
                return index >= 0 && index < _tableMetas.Count;
            }
            var parentTableMetas = ParentView.TotalFields;
            return index >= parentTableMetas.Count && index < parentTableMetas.Count + _tableMetas.Count;
        }
        
        private int ConvertToScopedIndex(int index)
        {
            if (ParentView == null)
                return index;
            var parentTableMetas = ParentView.TotalFields;
            return index - parentTableMetas.Count;
        }

        protected virtual void OnAddTableMeta(ODDBField field) { }
        protected virtual void OnRemoveField(int index) { }
        protected virtual void OnSwapTableMeta(int indexA, int indexB) { }
        public virtual bool TrySerialize(out string data)
        {
            data = null;
            var dtoBuilder = new ODDBViewDTOBuilder();
            var viewDto = dtoBuilder
                .SetName(this)
                .SetKey(this)
                .SetTableMeta(this)
                .SetBindType(this)
                .SetParentView(this)
                .Build();
            if (viewDto == null)
                return false;
            // serialize to json
            data = JsonConvert.SerializeObject(viewDto);
            return true;
        }

        public virtual bool TryDeserialize(string data)
        {
            var viewDto = JsonConvert.DeserializeObject<ODDBViewDTO>(data);
            if (viewDto == null)
                return false;
            Key = new ODDBID(viewDto.Key);
            Name = viewDto.Name;
            BindType = ODDBTypeUtility.TryConvertBindType(viewDto.BindType, out var bindType) ? bindType : null;
            _parentViewKey = new ODDBID(viewDto.ParentView);
            ScopedTableMetas.Clear();
            ScopedTableMetas.AddRange(viewDto.TableMetas);
            ODDBConverter.OnDatabaseCreated += OnDatabaseInitialize;
            return true;
        }
        

        public void OnDatabaseInitialize(ODDatabase database)
        {
            ParentView = database.Views.Read(_parentViewKey);
            database.OnDataChanged += OnDatabaseDataChanged;
            database.OnDataRemoved += OnDatabaseDataRemoved;
            ODDBConverter.OnDatabaseCreated -= OnDatabaseInitialize;
        }

        protected virtual void OnDatabaseDataChanged(ODDBID id)
        {
            if(ParentView != null && id == ParentView.Key)
                ParentView = ParentView;
        }
        
        protected virtual void OnDatabaseDataRemoved(ODDBID id) { }
    }
}