using UnityEditor;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Interface for custom property drawers in ODDB.
    /// </summary>
    public interface IODDBCellDrawer
    {
        /// <summary>
        /// Creates a custom VisualElement for the property.
        /// </summary>
        /// <param name="property"> The serialized property to create the GUI for.</param>
        /// <param name="typeKey"> The ODDB type key string of the property.</param>
        /// <param name="param"> Additional parameter for the data type.</param>
        /// <returns> A VisualElement representing the property.</returns>
        public VisualElement CreatePropertyGUI(SerializedProperty property, string typeKey, string param);
    }
}
