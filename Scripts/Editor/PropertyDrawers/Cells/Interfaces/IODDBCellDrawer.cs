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
        /// <returns> A VisualElement representing the property.</returns>
        public VisualElement CreatePropertyGUI(SerializedProperty property);
    }
}