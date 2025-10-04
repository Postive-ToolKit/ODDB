using System.Collections.Generic;
using TeamODD.ODDB.Runtime.DTO;
using TeamODD.ODDB.Runtime.DTO.Builders;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime
{
    public sealed class Table : View
    {
        private readonly List<Row> _rows = new();

        public List<Row> Rows => _rows;

        public Table()
        {
            OnFieldsChanged += ValidateRows;
        }
        
        private void ValidateRows()
        {
            foreach (var row in _rows)
            {
                row.ValidateTypes(TotalFields);
            }
        }

        protected override void OnAddTableMeta(Field field)
        {
            foreach (var row in _rows) {
                row.AddCell(field.Type);
            }
        }

        protected override void OnRemoveField(int index)
        {
            foreach (var row in _rows) {
                row.RemoveData(index);
            }
        }

        protected override void OnSwapTableMeta(int indexA, int indexB)
        {
            foreach (var row in _rows) {
                row.SwapData(indexA, indexB);
            }
        }
        
        public void AddRow()
        {
            _rows.Add(new Row(TotalFields));
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
            if (columnIndex < 0 || columnIndex >= TotalFields.Count) {
                return null;
            }
            var row = _rows[rowIndex];
            return row.GetData(columnIndex);
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
            ODDBConverter.OnDatabaseCreated += OnDatabaseInitialize;
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
                _rows.Add(new Row(id, TotalFields, data));
            }
            _cachedData = null;
        }

        #endregion
    }
}