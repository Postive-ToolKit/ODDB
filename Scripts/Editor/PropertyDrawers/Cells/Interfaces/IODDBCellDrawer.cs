using System;
using TeamODD.ODDB.Runtime;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Interface for custom property drawers in ODDB.
    /// </summary>
    public interface IODDBCellDrawer
    {
        /// <summary>
        /// Creates a VisualElement that edits the given cell.
        /// </summary>
        /// <param name="cell">The cell model to render.</param>
        /// <param name="typeKey">ODDB type key string of the cell.</param>
        /// <param name="param">Extra parameter for the data type.</param>
        /// <param name="commit">Invoked with a new serialized data string when the user edits the value.</param>
        VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit);
    }
}
