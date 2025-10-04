using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.DTO.Builders
{
    public class TableDTOBuilder : ViewDTOBuilder
    {
        private IReadOnlyList<Row> _rows;
        
        public TableDTOBuilder SetData(Table table)
        {
            _rows = table.Rows;
            return this;
        }
            
        public override ViewDTO Build()
        {
            var viewDto = base.Build();
            // convert as ODDBTableDTO
            var name = viewDto.Name;
            var key = viewDto.ID;
            var convertedMeta = viewDto.TableMetas;
            var convertedBindType = viewDto.BindType;
            var parentView = viewDto.ParentView;
            var data = new string[_rows.Count][];
            for (int i = 0; i < _rows.Count; i++)
            {
                var rowData = new List<string>();
                rowData.Add(_rows[i].ID.ToString());
                rowData.AddRange(_rows[i].Cells.Select(cell => cell.SerializedData));
                data[i] = rowData.ToArray();
            }
            
            return new TableDTO(name, key, convertedMeta, convertedBindType, parentView,data);
        }
    }
}