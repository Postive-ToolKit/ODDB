using System;
using System.Collections.Generic;
using Plugins.ODDB.Scripts.Runtime.Data.Interfaces;
using TeamODD.ODDB.Scripts.Runtime.Data;

namespace Plugins.ODDB.Scripts.Runtime.Data
{
    public class ODDBView : IODDBHasUniqueKey, IODDBHasName, IODDBHasTableMeta, IODDBHasBindType, IODDBTableMetaHandler
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public Type BindType { get; set; }
        public List<ODDBTableMeta> TableMetas => _tableMetas;
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
            _tableMetas.RemoveAt(index);
            OnRemoveTableMeta(index);
        }
        
        public void SwapTableMeta(int indexA, int indexB)
        {
            (_tableMetas[indexA], _tableMetas[indexB]) = (_tableMetas[indexB], _tableMetas[indexA]);
            OnSwapTableMeta(indexA, indexB);
        }
        
        protected virtual void OnAddTableMeta(ODDBTableMeta tableMeta) { }
        protected virtual void OnRemoveTableMeta(int index) { }
        protected virtual void OnSwapTableMeta(int indexA, int indexB) { }
    }
}