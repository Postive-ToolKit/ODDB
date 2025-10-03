using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data;
using UnityEngine;

namespace TeamODD.ODDB.Editors.DTO
{
    /// <summary>
    /// Wrapper class for view metadata
    /// </summary>
    public class ViewMetaDTO : ScriptableObject
    {
        public List<ODDBField> TableMetas = new List<ODDBField>();
    }
}