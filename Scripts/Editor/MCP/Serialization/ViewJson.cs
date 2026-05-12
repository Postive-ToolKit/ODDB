using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.MCP.Serialization
{
    public static class ViewJson
    {
        public static JObject Field(Field f) => new JObject
        {
            ["name"] = f.Name,
            ["type"] = f.Type.Type.ToString(),
            ["param"] = f.Type.Param,
        };

        public static JArray Fields(IEnumerable<Field> fs)
        {
            var arr = new JArray();
            if (fs == null) return arr;
            foreach (var f in fs) arr.Add(Field(f));
            return arr;
        }
    }
}
