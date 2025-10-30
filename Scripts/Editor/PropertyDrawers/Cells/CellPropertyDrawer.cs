using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(Cell))]
    public class CellPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var fieldTypeProp = property.FindPropertyRelative(Cell.DATA_TYPE_FIELD);
            var dataType = (ODDBDataType)fieldTypeProp.FindPropertyRelative(FieldType.TYPE_FIELD).enumValueFlag;
            var param = fieldTypeProp.FindPropertyRelative(FieldType.PARAM_FIELD).stringValue;
            return dataType.GetCellDrawer().CreatePropertyGUI(property, dataType, param);
        }
    }
}