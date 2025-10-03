using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDBFieldInfo : IODDBHasTableMeta, IODDBTableMetaHandler
    {
        public event Action<ODDBField> OnAddTableMeta;
        public event Action<int> OnRemoveField;
        public event Action<int, int> OnSwapTableMeta;
        public event Action<ODDBFieldInfo> OnParentChanged;

        public List<ODDBField> TotalFields
        {
            get
            {
                if (Parent == null)
                    return _fields;
                var parentTableMetas = Parent.TotalFields;
                var tableMetas = new List<ODDBField>();
                tableMetas.AddRange(parentTableMetas);
                tableMetas.AddRange(_fields);
                return tableMetas;
            }
        }
        public List<ODDBField> ScopedFields => _fields;

        public ODDBFieldInfo Parent
        {
            get => _parent;
            set
            {
                _parent = value;
                OnParentChanged?.Invoke(_parent);
            }
        }
        private ODDBFieldInfo _parent;
        private List<ODDBField> _fields = new();
        
        public void AddField(ODDBField field)
        {
            _fields.Add(field);
            OnAddTableMeta?.Invoke(field);
        }
        
        public void RemoveField(int index)
        {
            if (!IsScopedMeta(index)) {
                Debug.LogError($"Index {index} is out of range for this view.");
                return;
            }
            _fields.RemoveAt(ConvertToScopedIndex(index));
            OnRemoveField?.Invoke(index);
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
            OnSwapTableMeta?.Invoke(indexA, indexB);
        }

        public bool IsScopedMeta(int index)
        {
            if (Parent == null)
            {
                return index >= 0 && index < TotalFields.Count;
            }
            var parentTableMetas = Parent.TotalFields;
            return index >= parentTableMetas.Count && index < parentTableMetas.Count + Parent.TotalFields.Count;
        }
        
        private int ConvertToScopedIndex(int index)
        {
            if (Parent == null)
                return index;
            return index - Parent.TotalFields.Count;
        }
    }
}