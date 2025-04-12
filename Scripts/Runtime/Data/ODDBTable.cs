using System;
using System.Collections.Generic;
using System.Reflection;
using Plugins.ODDB.Scripts.Runtime.Data.Enum;
using Plugins.ODDB.Scripts.Runtime.Data.Interfaces;

namespace TeamODD.ODDB.Scripts.Runtime.Data
{
    public class ODDBTable : IODDBHasUniqueKey, IODDBHasName, IODDBHasTableMeta, IODDBAvailableSerialize
    {
        public string Key { get; set; }
        public string Name { get; set; }
        private readonly List<ODDBTableMeta> _tableMetas = new List<ODDBTableMeta>();
        private readonly List<ODDBRow> _rows = new List<ODDBRow>();
        public List<ODDBTableMeta> TableMetas => _tableMetas;
        
        public IReadOnlyList<ODDBRow> ReadOnlyRows => _rows.AsReadOnly();
        
        public ODDBTable(IEnumerable<ODDBTableMeta> tableMetas = null)
        {
            if (tableMetas == null)
                return;
            _tableMetas.AddRange(tableMetas);
        }
        public void AddTableMeta(ODDBTableMeta tableMeta)
        {
            _tableMetas.Add(tableMeta);
            // TODO : Add all data of index in this table
            foreach (var row in _rows)
            {
                row.InsertData(_tableMetas.Count - 1, null);
            }
        }
        public void RemoveTableMeta(int index)
        {
            _tableMetas.RemoveAt(index);
            // TODO : Remove all data of index in this table
            foreach (var row in _rows)
            {
                row.RemoveData(index);
            }
        }
        
        public void SwapTableMeta(int indexA, int indexB)
        {
            (_tableMetas[indexA], _tableMetas[indexB]) = (_tableMetas[indexB], _tableMetas[indexA]);
            foreach (var row in _rows)
            {
                row.SwapData(indexA, indexB);
            }
        }
        
        public void AddRow(ODDBRow row = null)
        {
            if (row == null)
            {
                row = new ODDBRow(_tableMetas.Count);
            }
            _rows.Add(row);
        }
        
        public void InsertRow(int index, ODDBRow row)
        {
            _rows.Insert(index, row);
        }
        
        public void SwapRow(int indexA, int indexB)
        {
            (_rows[indexA], _rows[indexB]) = (_rows[indexB], _rows[indexA]);
        }
        
        public void RemoveRow(int index)
        {
            _rows.RemoveAt(index);
        }
        
        public void ClearRows()
        {
            _rows.Clear();
        }
        
        
        public object GetValue(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= _rows.Count) {
                return null;
            }
            if (columnIndex < 0 || columnIndex >= _tableMetas.Count) {
                return null;
            }
            var row = _rows[rowIndex];
            return row.GetData(columnIndex);
        }
        public Type GetTypeOfColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _tableMetas.Count) {
                return null;
            }
            var tableMeta = _tableMetas[columnIndex];
            return ODDBTypeMapper.GetEnumType(tableMeta.DataType);
        }

        public string Serialize()
        {
            var data = string.Empty;
            foreach (var tableMeta in _tableMetas)
            {
                data += tableMeta.Name + ",";
            }
            data = data.TrimEnd(',');
            data += "\n";
            
            foreach (var row in _rows)
            {
                for (int i = 0; i < _tableMetas.Count; i++)
                {
                    var value = row.GetData(i);
                    data += value + ",";
                }
                data = data.TrimEnd(',');
                data += "\n";
            }
            return data;
        }
    }
}