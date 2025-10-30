using TeamODD.ODDB.Runtime.Enums;
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
        /// <param name="dataType"> The ODDB data type of the property.</param>
        /// <param name="param"> Additional parameter for the data type.</param>
        /// <returns> A VisualElement representing the property.</returns>
        public VisualElement CreatePropertyGUI(SerializedProperty property, ODDBDataType dataType, string param);
        
        
    }
}