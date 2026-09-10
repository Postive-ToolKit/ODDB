using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.DTO;
using TeamODD.ODDB.Runtime.DTO.Builders;
using TeamODD.ODDB.Runtime.Utils.Converters;

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

        protected override void OnMoveField(int oldIndex, int newIndex)
        {
            foreach (var row in Rows)
            {
                row.MoveData(oldIndex, newIndex);
            }
        }

        public Row AddRow()
        {
            return AddRow(new ODDBID());
        }

        public Row AddRow(ODDBID id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var key = id.ToString();
            if (_rows.ContainsKey(key))
                throw new InvalidOperationException($"Row ID '{key}' already exists.");

            var newRow = new Row(TotalFields)
            {
                ID = id
            };
            _rows.Add(key, newRow);
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

        public bool RekeyRow(string oldId, string newId)
        {
            if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId))
                return false;
            if (string.Equals(oldId, newId, StringComparison.Ordinal))
                return false;
            if (!_rows.TryGetValue(oldId, out var row) || _rows.ContainsKey(newId))
                return false;

            var orderedRows = _rows.Values.ToList();
            row.ID = new ODDBID(newId);
            _rows.Clear();
            foreach (var orderedRow in orderedRows)
                _rows.Add(orderedRow.ID, orderedRow);

            OnRowChanged?.Invoke();
            return true;
        }

        public Cell GetCell(string rowId, int fieldIndex)
        {
            if (string.IsNullOrEmpty(rowId))
                return null;
            return GetRow(rowId)?.GetData(fieldIndex);
        }

        public bool SetCellData(string rowId, int fieldIndex, object value, bool direct = false)
        {
            var cell = GetCell(rowId, fieldIndex);
            if (cell == null)
                return false;

            cell.SetData(value, direct);
            OnRowChanged?.Invoke();
            return true;
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
            UnresolvedBindType = string.Empty;
            BindType = ODDBTypeUtility.TryConvertBindType(tableDto.BindType, out var bindType) ? bindType : null;
            if (BindType == null && !string.IsNullOrWhiteSpace(tableDto.BindType))
                UnresolvedBindType = tableDto.BindType;
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
                if (_rows.ContainsKey(id))
                {
                    ODDB.Logger.Warn($"Duplicate row ID found: {id}. Skipping this row.");
                    continue;
                }
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
