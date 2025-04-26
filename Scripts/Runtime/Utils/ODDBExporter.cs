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
        public string Export(ODDatabase database)
        {
            if (database == null)
            {
                Debug.LogError("ODDBExporter.Export cannot export null database");
                return default;
            }

            if (!database.TrySerialize(out var data))
                return null;
            return data;
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