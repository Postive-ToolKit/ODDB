using UnityEngine;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Inspector attribute to select an ODDB Entity by its ID.
    /// </summary>
    public class ODDBIDSelectorAttribute : PropertyAttribute
    {
        /// <summary>
        /// Allowed tables for selection.
        /// </summary>
        public string[] AllowTables { get; }
        
        /// <summary>
        /// Constructor for ODDBIDSelectorAttribute.
        /// </summary>
        /// <param name="allowTables"> Optional list of allowed tables for selection.</param>
        public ODDBIDSelectorAttribute(params string[] allowTables)
        {
            AllowTables = allowTables;
        }
    }
}