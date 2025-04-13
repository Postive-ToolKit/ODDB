using System;
using System.Collections.Generic;
using Plugins.ODDB.Scripts.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Settings.Data;
using TeamODD.ODDB.Scripts.Runtime.Data;

namespace TeamODD.ODDB.Runtime
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
            return new ODDatabaseDTO(tables);
        }

        private bool TryConvertTable(ODDBTable table, out ODDBTableDTO tableDto)
        {
            try
            {
                var dtoBuilder = new ODDBTableDTO.Builder();
                tableDto = dtoBuilder
                    .SetName(table)
                    .SetKey(table)
                    .SetSerialization(table)
                    .SetTableMeta(table)
                    .SetBindType(table)
                    .Build();
                return true;
            }
            catch (Exception e)
            {
                tableDto = default;
                return false;
            }
        }
    }
}