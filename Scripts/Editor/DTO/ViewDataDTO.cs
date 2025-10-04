using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime;
using UnityEngine;

namespace TeamODD.ODDB.Editors.DTO
{
    /// <summary>
    /// Wrapper class for view metadata
    /// </summary>
    public class ViewDataDTO : ScriptableObject
    {
        public event Action OnFieldsChanged; 
        
        public List<Field> Fields = new List<Field>();

        private void OnValidate()
        {
            OnFieldsChanged?.Invoke();
        }
    }
}