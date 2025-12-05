using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.DTO;
using TeamODD.ODDB.Runtime.DTO.Builders;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    public sealed class Table : View
    {
        public event Action OnRowChanged;
        private readonly Dictionary<string, Row> _rows = new();

        public List<Row> Rows => _rows.Values.ToList();

        public Table()
        {
            OnFieldsChanged += ValidateRows;
            OnFieldAdded += OnAddField;
        }
        
        private void ValidateRows()
        {
            foreach (var row in _rows.Values.ToList())
                row.ValidateTypes(TotalFields);
            OnRowChanged?.Invoke();
        }

        protected override void OnAddField(Field field)
        {
            foreach (var row in Rows) 
                row.AddCell(field.Type);
        }

        protected override void OnRemoveField(int index)
        {
            foreach (var row in Rows) 
                row.RemoveData(index);
        }

        protected override void OnSwapTableMeta(int indexA, int indexB)
        {
            foreach (var row in Rows) 
            {
                row.SwapData(indexA, indexB);
            }
        }
        
        public Row AddRow()
        {
            var newRow = new Row(TotalFields);
            _rows.Add(newRow.ID, newRow);
            OnRowChanged?.Invoke();
            return newRow;
        }

        public void RestoreRow(Row row)
        {
            if (_rows.ContainsKey(row.ID)) return;
            _rows.Add(row.ID, row);
            OnRowChanged?.Invoke();
        }

        public void RemoveRow(int index)
        {
            var row = Rows.ElementAtOrDefault(index);
            RemoveRow(row?.ID.ToString());
        }
        
        public void RemoveRow(string rowId)
        {
            if (string.IsNullOrEmpty(rowId) || !_rows.ContainsKey(rowId))
                return;
            _rows.Remove(rowId);
            OnRowChanged?.Invoke();
        }
        
        public Row GetRow(string rowId)
        {
            _rows.TryGetValue(rowId, out var row);
            return row;
        }
        
        public void Clear()
        {
            _rows.Clear();
            OnRowChanged?.Invoke();
        }

        #region Serialization

        public override ViewDTO ToDTO()
        {
            var dtoBuilder = new TableDTOBuilder();
            var viewDto = dtoBuilder
                .SetData(this)
                .SetName(this)
                .SetID(this)
                .SetTableMeta(this)
                .SetBindType(this)
                .SetParentView(this)
                .Build();
            return viewDto;
        }

        private string[][] _cachedData = null;
        public override void FromDTO(ViewDTO dto)
        {
            if (dto is not TableDTO tableDto)
                return;

            ID = new ODDBID(tableDto.ID);
            Name = tableDto.Name;
            BindType = ODDBTypeUtility.TryConvertBindType(tableDto.BindType, out var bindType) ? bindType : null;
            _parentViewKey = new ODDBID(tableDto.ParentView);

            ScopedFields.Clear();
            if (tableDto.TableMetas != null)
                ScopedFields.AddRange(tableDto.TableMetas);
            
            _rows.Clear();
            _cachedData = tableDto.Data;
            ODDBConverter.OnDatabaseCreated.Add(new DataBaseCreateEvent
            {
                Priority = DataCreateProcess.TableRowData,
                OnEvent = OnDatabaseInitialize,
            });
        }

        public override void OnDatabaseInitialize(ODDatabase database)
        {
            base.OnDatabaseInitialize(database);
            if (_cachedData == null)
                return;
            
            foreach (var rowData in _cachedData)
            {
                var id = new ODDBID(rowData[0]);
                var data = new string[rowData.Length - 1];
                for (int i = 1; i < rowData.Length; i++)
                    data[i - 1] = rowData[i];
                var row = new Row(id, TotalFields, data);
                _rows.Add(row.ID, row);
            }
            _cachedData = null;
        }

        #endregion
    }
}