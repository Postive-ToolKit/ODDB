using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Enum;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    [Serializable]
    public class ODDBRow : IODDBHasUniqueID
    {
        public const string ID_FIELD = nameof(_id);
        public const string CELLS_FIELD = nameof(_cells);
        
        public ODDBID ID
        {
            get => _id;
            set => _id = value;
        }
        public IReadOnlyList<ODDBCell> Cells => _cells;
        
        [SerializeField] private List<ODDBCell> _cells = new();
        [SerializeField] private ODDBID _id = new ODDBID();
        public ODDBRow(List<ODDBField> fields)
        {
            ID = new ODDBID();
            for (int i = 0; i < fields.Count; i++)
                _cells.Add(new ODDBCell(fields[i].Type));
        }
        public ODDBRow(ODDBID id, List<ODDBField> fields, string[] data)
        {
            ID = id;
            for (int i = 0; i < fields.Count; i++)
            {
                var serializedData = i < data.Length ? data[i] : string.Empty;
                _cells.Add(new ODDBCell(serializedData, fields[i].Type));
            }
        }
        
        public ODDBCell GetData(int index)
        {
            if (index >= _cells.Count || index < 0)
                return null;
            return _cells[index];
        }
        
        public void SetData(int index, string data)
        {
            if (index >= _cells.Count || index < 0)
                return;
            _cells[index].SetData(data);
        }
        
        public void RemoveData(int index)
        {
            if (index >= _cells.Count || index < 0)
                return;
            _cells.RemoveAt(index);
        }

        public void SwapData(int indexA, int indexB)
        {
            if (indexA >= 0 && indexA < _cells.Count && indexB >= 0 && indexB < _cells.Count)
            {
                (_cells[indexA], _cells[indexB]) = (_cells[indexB], _cells[indexA]);
            }
        }

        public void AddCell(ODDBDataType type)
        {
            _cells.Add(new ODDBCell(type));
        }
    }
}