using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Data.DTO.Builders;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data
{
    public sealed class ODDBTable : ODDBView
    {
        private readonly List<ODDBRow> _rows = new();

        public List<ODDBRow> Rows => _rows;

        public ODDBTable()
        {
            
        }

        protected override void OnAddTableMeta(ODDBField field)
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
            _rows.Add(new ODDBRow(TotalFields));
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

        public override ODDBViewDTO ToDTO()
        {
            var dtoBuilder = new ODDBTableDTOBuilder();
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
        public override void FromDTO(ODDBViewDTO dto)
        {
            if (dto is not ODDBTableDTO tableDto)
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
                _rows.Add(new ODDBRow(id, TotalFields, data));
            }
            _cachedData = null;
        }

        #endregion
    }
}