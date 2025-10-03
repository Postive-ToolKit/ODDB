using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Data.DTO.Builders;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDBView : IODDBView
    {
        private const string DEFAULT_NAME = "Default Name";
        public ODDBID ID { get; set; }
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
                if (_parentView == this)
                {
                    Debug.LogError("Cannot set parent view to itself.");
                    return;
                }
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
                    return _fields;
                var parentTableMetas = ParentView.TotalFields;
                var tableMetas = new List<ODDBField>();
                tableMetas.AddRange(parentTableMetas);
                tableMetas.AddRange(_fields);
                return tableMetas;
            }
        }
        public List<ODDBField> ScopedFields => _fields;
        private readonly List<ODDBField> _fields = new();
        
        public ODDBView()
        {
            ID = new ODDBID();
            Name = DEFAULT_NAME;
        }
        public ODDBView(IEnumerable<ODDBField> tableMetas = null)
        {
            if (tableMetas == null)
                return;
            _fields.AddRange(tableMetas);
        }
        
        public void AddField(ODDBField field)
        {
            _fields.Add(field);
            OnAddTableMeta(field);
        }
        
        public void RemoveField(int index)
        {
            if (!IsScopedMeta(index)) {
                Debug.LogError($"Index {index} is out of range for this view.");
                return;
            }
            _fields.RemoveAt(ConvertToScopedIndex(index));
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
            (_fields[scopedIndexA], _fields[scopedIndexB]) = (_fields[scopedIndexB], _fields[scopedIndexA]);
            OnSwapTableMeta(indexA, indexB);
        }

        public bool IsScopedMeta(int index)
        {
            if (ParentView == null)
            {
                return index >= 0 && index < _fields.Count;
            }
            var parentTableMetas = ParentView.TotalFields;
            return index >= parentTableMetas.Count && index < parentTableMetas.Count + _fields.Count;
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

        public virtual void OnDatabaseInitialize(ODDatabase database)
        {
            ParentView = database.Views.Read(_parentViewKey);
            database.OnDataChanged += OnDatabaseDataChanged;
            database.OnDataRemoved += OnDatabaseDataRemoved;
            ODDBConverter.OnDatabaseCreated -= OnDatabaseInitialize;
        }

        protected virtual void OnDatabaseDataChanged(ODDBID id)
        {
            if(ParentView != null && id == ParentView.ID)
                ParentView = ParentView;
        }
        
        protected virtual void OnDatabaseDataRemoved(ODDBID id) { }
        public virtual ODDBViewDTO ToDTO()
        {
            var dtoBuilder = new ODDBViewDTOBuilder();
            var viewDto = dtoBuilder
                .SetName(this)
                .SetID(this)
                .SetTableMeta(this)
                .SetBindType(this)
                .SetParentView(this)
                .Build();
            
            return viewDto;
        }

        public virtual void FromDTO(ODDBViewDTO dto)
        {
            if (dto == null)
                return;
            
            ID = new ODDBID(dto.ID);
            Name = dto.Name;
            BindType = ODDBTypeUtility.TryConvertBindType(dto.BindType, out var bindType) ? bindType : null;

            _parentViewKey = new ODDBID(dto.ParentView);
            ScopedFields.Clear();
            ScopedFields.AddRange(dto.TableMetas);
            ODDBConverter.OnDatabaseCreated += OnDatabaseInitialize;
        }
    }
}