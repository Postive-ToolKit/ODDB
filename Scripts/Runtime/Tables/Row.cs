using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    [Serializable]
    public class Row : IHasODDBID
    {
        public const string ID_FIELD = nameof(_id);
        public const string CELLS_FIELD = nameof(_cells);
        
        public ODDBID ID
        {
            get => _id;
            set => _id = value;
        }
        public IReadOnlyList<Cell> Cells => _cells;
        
        [SerializeField] private List<Cell> _cells = new();
        [SerializeField] private ODDBID _id = new ODDBID();
        public Row(List<Field> fields)
        {
            ID = new ODDBID();
            for (int i = 0; i < fields.Count; i++)
                _cells.Add(new Cell(fields[i].Type));
        }
        public Row(ODDBID id, List<Field> fields, string[] data)
        {
            ID = id;
            for (int i = 0; i < fields.Count; i++)
            {
                var serializedData = i < data.Length ? data[i] : string.Empty;
                _cells.Add(new Cell(serializedData, fields[i].Type));
            }
        }
        
        public Cell GetData(int index)
        {
            if (index >= _cells.Count || index < 0)
                return null;
            return _cells[index];
        }
        
        public void SetData(int index, string data, bool direct = false)
        {
            if (index >= _cells.Count || index < 0)
                return;
            _cells[index].SetData(data, direct);
        }
        
        public void RemoveData(int index)
        {
            if (index >= _cells.Count || index < 0)
                return;
            _cells.RemoveAt(index);
        }

        public void MoveData(int oldIndex, int newIndex)
        {
            if (oldIndex >= 0 && oldIndex < _cells.Count && newIndex >= 0 && newIndex < _cells.Count)
            {
                var item = _cells[oldIndex];
                _cells.RemoveAt(oldIndex);
                _cells.Insert(newIndex, item);
            }
        }

        public void AddCell(FieldType type)
        {
            _cells.Add(new Cell(type));
        }

        public void ValidateTypes(List<Field> totalFields)
        {
            while (_cells.Count > totalFields.Count)
                _cells.RemoveAt(0);

            while (totalFields.Count > _cells.Count)
                _cells.Insert(0, new Cell());
            
            for (int i = 0; i < totalFields.Count; i++)
                _cells[i].FieldType = totalFields[i].Type;
        }

        /// <summary>
        /// Runtime row name — always the ID. Editor display preference
        /// (UseFirstColumnAsRowName) is applied by the Editor layer via
        /// RowDisplayName.For(row) — see Scripts/Editor/Utils/RowDisplayName.cs.
        /// </summary>
        public string GetName()
        {
            return ID.ToString();
        }
    }
}