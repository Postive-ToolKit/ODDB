using System.Collections.Generic;
using TeamODD.ODDB.Runtime;
using UnityEngine;

namespace TeamODD.ODDB.Editors.DTO
{
    public class TableDataDTO : ScriptableObject
    {
        public List<Row> Rows = new();
    }
}