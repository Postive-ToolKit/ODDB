using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.DTO;
using TeamODD.ODDB.Runtime.DTO.Builders;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine;
using UnityEngine.Assertions;

namespace TeamODD.ODDB.Runtime
{
    public class View : IView
    {
        public event Action OnFieldsChanged;
        
        public event Action<Field> OnFieldAdded;
        
        private const string DEFAULT_NAME = "Default Name";
        public ODDBID ID { get; set; }
        public string Name { get; set; }
        public Type BindType { get; set; }

        public IView ParentView
        {
            get => _parentView;
            set
            {
                Assert.IsFalse(_parentView == this, "Cannot set parent view to itself.");
                if (_parentView != null)
                {
                    _parentView.OnFieldsChanged -= NotifyFieldsChanged;
                    _parentView.OnFieldAdded -= NotifyFieldAdded;
                }
                _parentView = value;
                NotifyFieldsChanged();
                if(_parentView == null)
                    return;
                _parentView.OnFieldsChanged += NotifyFieldsChanged;
                _parentView.OnFieldAdded += NotifyFieldAdded;
                
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

        private IView _parentView;
        
        public List<Field> TotalFields
        {
            get
            {
                if (ParentView == null)
                    return _fields;
                var parentTableMetas = ParentView.TotalFields;
                var tableMetas = new List<Field>();
                tableMetas.AddRange(parentTableMetas);
                tableMetas.AddRange(_fields);
                return tableMetas;
            }
        }
        public List<Field> ScopedFields => _fields;
        private readonly List<Field> _fields = new();
        
        public View()
        {
            ID = new ODDBID();
            Name = DEFAULT_NAME;
        }
        public View(IEnumerable<Field> tableMetas = null)
        {
            if (tableMetas == null)
                return;
            _fields.AddRange(tableMetas);
        }

        public void AddField(Field field)
        {
            _fields.Add(field);
            OnAddField(field);
            OnFieldAdded?.Invoke(field);
        }
        
        public void InsertField(int index, Field field)
        {
            int myStartIndex = 0;
            if (ParentView != null) 
                myStartIndex = ParentView.TotalFields.Count;
             
            int scopedIndex = index - myStartIndex;
             
            if (scopedIndex < 0 || scopedIndex > _fields.Count)
            {
                Debug.LogError($"Index {index} is out of range for insertion in this view.");
                return;
            }
             
            _fields.Insert(scopedIndex, field);
            OnAddField(field);
            OnFieldAdded?.Invoke(field);
        }
        
        public void RemoveField(int index)
        {
            if (!IsScopedField(index)) {
                Debug.LogError($"Index {index} is out of range for this view.");
                return;
            }
            _fields.RemoveAt(ConvertToScopedIndex(index));
            OnRemoveField(index);
        }
        
        public void SwapFields(int indexA, int indexB)
        {
            if (!IsScopedField(indexA) || !IsScopedField(indexB))
            {
                Debug.LogError($"Index {indexA} or {indexB} is out of range for this view.");
                return;
            }
            var scopedIndexA = ConvertToScopedIndex(indexA);
            var scopedIndexB = ConvertToScopedIndex(indexB);
            (_fields[scopedIndexA], _fields[scopedIndexB]) = (_fields[scopedIndexB], _fields[scopedIndexA]);
            OnSwapTableMeta(indexA, indexB);
        }

        public bool IsScopedField(int index)
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

        #region Virtual Event Methods
        protected virtual void OnAddField(Field field) { }
        protected virtual void OnRemoveField(int index) { }
        protected virtual void OnSwapTableMeta(int indexA, int indexB) { }

        public virtual void OnDatabaseInitialize(ODDatabase database)
        {
            ParentView = database.Views.Read(_parentViewKey);
            database.OnDataChanged += OnDatabaseDataChanged;
            database.OnDataRemoved += OnDatabaseDataRemoved;
        }

        protected virtual void OnDatabaseDataChanged(ODDBID id)
        {
            if(ParentView != null && id == ParentView.ID)
                ParentView = ParentView;
        }
        
        protected virtual void OnDatabaseDataRemoved(ODDBID id) { }
        #endregion
        
        public bool IsChildOf(string viewId)
        {
            if (ParentView == null)
                return false;
            if (ParentView.ID == viewId)
                return true;
            return ParentView.IsChildOf(viewId);
        }
        
        public virtual ViewDTO ToDTO()
        {
            var dtoBuilder = new ViewDTOBuilder();
            var viewDto = dtoBuilder
                .SetName(this)
                .SetID(this)
                .SetTableMeta(this)
                .SetBindType(this)
                .SetParentView(this)
                .Build();
            
            return viewDto;
        }

        public virtual void FromDTO(ViewDTO dto)
        {
            if (dto == null)
                return;
            
            ID = new ODDBID(dto.ID);
            Name = dto.Name;
            BindType = ODDBTypeUtility.TryConvertBindType(dto.BindType, out var bindType) ? bindType : null;

            _parentViewKey = new ODDBID(dto.ParentView);
            ScopedFields.Clear();
            ScopedFields.AddRange(dto.TableMetas);
            ODDBConverter.OnDatabaseCreated.Add(new DataBaseCreateEvent
            {
                Priority = DataCreateProcess.ViewFieldInfo,
                OnEvent = OnDatabaseInitialize
            });
        }
        
        public void NotifyFieldsChanged()
        {
            OnFieldsChanged?.Invoke();
        }

        public void NotifyFieldAdded(Field field)
        {
            OnFieldAdded?.Invoke(field);
        }
    }
}