using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Interface for custom property drawers in ODDB.
    /// </summary>
    public interface IODDBCellDrawer
    {
        /// <summary>
        /// Draws the property in the Unity Inspector.
        /// </summary>
        /// <param name="position"> The position to draw the property.</param>
        /// <param name="property"> The serialized property to be drawn.</param>
        /// <param name="label"> The label for the property.</param>
        public void OnGUI(Rect position, SerializedProperty property, GUIContent label);
    }
}