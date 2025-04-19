using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Data.DTO.Builders;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Utils
{
    public class ODDBExporter
    {
        public ODDatabaseDTO Export(ODDatabase database)
        {
            var tables = new List<ODDBTableDTO>();
            foreach (var table in database.Tables)
            {
                if (TryConvertTable(table, out var tableDto))
                {
                    tables.Add(tableDto);
                }
                
            }
            var views = new List<ODDBViewDTO>();
            foreach (var view in database.Views)
            {
                if (TryConvertView(view, out var viewDto))
                {
                    views.Add(viewDto);
                }
            }
            return new ODDatabaseDTO(tables, views);
        }
        
        private bool TryConvertView(ODDBView view, out ODDBViewDTO viewDto)
        {
            try
            {
                var dtoBuilder = new ODDBViewDTOBuilder();
                viewDto = dtoBuilder
                    .SetName(view)
                    .SetKey(view)
                    .SetTableMeta(view)
                    .SetBindType(view)
                    .SetParentView(view)
                    .Build();
                return true;
            }
            catch (Exception e)
            {
                viewDto = default;
                Debug.LogError("ODDBExporter.TryConvertView cannot convert view to dto : " + e.Message);
                return false;
            }
        }

        private bool TryConvertTable(ODDBTable table, out ODDBTableDTO tableDto)
        {
            try
            {
                var dtoBuilder = new ODDBTableDTOBuilder();
                tableDto = dtoBuilder
                    .SetSerialization(table)
                    .SetName(table)
                    .SetKey(table)
                    .SetTableMeta(table)
                    .SetBindType(table)
                    .SetParentView(table)
                    .Build() as ODDBTableDTO;
                return true;
            }
            catch (Exception e)
            {
                tableDto = default;
                Debug.LogError("ODDBExporter.TryConvertTable cannot convert table to dto : " + e.Message);
                return false;
            }
        }
    }
}