using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDBView : IODDBView
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public Type BindType { get; set; }
        public IODDBView ParentView { get; set; }

        public List<ODDBTableMeta> TableMetas
        {
            get
            {
                if (ParentView == null)
                    return _tableMetas;
                var parentTableMetas = ParentView.TableMetas;
                var tableMetas = new List<ODDBTableMeta>();
                tableMetas.AddRange(parentTableMetas);
                tableMetas.AddRange(_tableMetas);
                return tableMetas;
            }
        }
        public List<ODDBTableMeta> ScopedTableMetas => _tableMetas;
        private readonly List<ODDBTableMeta> _tableMetas = new();
        public ODDBView(IEnumerable<ODDBTableMeta> tableMetas = null)
        {
            if (tableMetas == null)
                return;
            _tableMetas.AddRange(tableMetas);
        }
        
        public void AddField(ODDBTableMeta tableMeta)
        {
            _tableMetas.Add(tableMeta);
            OnAddTableMeta(tableMeta);
        }
        
        public void RemoveTableMeta(int index)
        {
            if (!IsScopedMeta(index)) {
                Debug.LogError($"Index {index} is out of range for this view.");
                return;
            }
            _tableMetas.RemoveAt(index);
            OnRemoveTableMeta(index);
        }
        
        public void SwapTableMeta(int indexA, int indexB)
        {
            if (!IsScopedMeta(indexA) || !IsScopedMeta(indexB))
            {
                Debug.LogError($"Index {indexA} or {indexB} is out of range for this view.");
                return;
            }
                
            (_tableMetas[indexA], _tableMetas[indexB]) = (_tableMetas[indexB], _tableMetas[indexA]);
            OnSwapTableMeta(indexA, indexB);
        }

        public bool IsScopedMeta(int index)
        {
            if (ParentView == null)
            {
                return index >= 0 && index < _tableMetas.Count;
            }
            var parentTableMetas = ParentView.TableMetas;
            return index >= parentTableMetas.Count && index < parentTableMetas.Count + _tableMetas.Count;
        }

        protected virtual void OnAddTableMeta(ODDBTableMeta tableMeta) { }
        protected virtual void OnRemoveTableMeta(int index) { }
        protected virtual void OnSwapTableMeta(int indexA, int indexB) { }
    }
}