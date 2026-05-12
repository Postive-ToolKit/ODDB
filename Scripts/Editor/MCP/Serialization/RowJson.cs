using System;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.MCP.Serialization
{
    public static class RowJson
    {
        public static JObject Row(Row row)
        {
            var cells = new JArray();
            int n = row.Cells?.Count ?? 0;
            for (int i = 0; i < n; i++)
            {
                var cell = row.GetData(i);
                if (cell == null) { cells.Add(JValue.CreateNull()); continue; }
                cells.Add(Cell(cell));
            }
            return new JObject
            {
                ["id"] = row.ID.ToString(),
                ["cells"] = cells,
            };
        }

        public static JObject Cell(Cell cell)
        {
            object deserialized = null;
            try { deserialized = cell.GetData(); }
            catch (Exception) { deserialized = null; }
            return new JObject
            {
                ["value"] = cell.SerializedData,
                ["deserialized"] = deserialized == null ? null : JToken.FromObject(deserialized),
                ["fieldType"] = new JObject
                {
                    ["type"] = cell.FieldType.Type.ToString(),
                    ["param"] = cell.FieldType.Param,
                },
            };
        }
    }
}
