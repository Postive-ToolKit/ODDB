using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enum;
using UnityEditor;
using UnityEngine;
namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(Cell))]
    public class CellPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // FieldType SerializedProperty
            var fieldTypeProp = property.FindPropertyRelative(Cell.DATA_TYPE_FIELD);
            var dataType = (ODDBDataType)fieldTypeProp.FindPropertyRelative(FieldType.TYPE_FIELD).enumValueFlag;
            dataType.GetCellDrawer().OnGUI(position, property, label);
        }
    }
}