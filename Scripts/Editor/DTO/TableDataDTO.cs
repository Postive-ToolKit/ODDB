using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data;
using UnityEngine;

namespace TeamODD.ODDB.Editors.DTO
{
    public class TableDataDTO : ScriptableObject
    {
        public List<ODDBRow> Rows = new();
    }
}