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
            var typeKeyProp = fieldTypeProp.FindPropertyRelative(FieldType.TYPE_KEY_FIELD);
            var paramProp = fieldTypeProp.FindPropertyRelative(FieldType.PARAM_FIELD);
            var dataType = (ODDBDataType)fieldTypeProp.FindPropertyRelative(FieldType.TYPE_FIELD).enumValueFlag;

            string typeKey = !string.IsNullOrEmpty(typeKeyProp?.stringValue)
                ? typeKeyProp.stringValue
                : dataType.ToWireKey();

            var drawer = CellDrawerRegistry.Get(typeKey);
            if (drawer == null)
            {
                // Fall back to legacy enum-based lookup while migration is in progress.
                drawer = dataType.GetCellDrawer(paramProp.stringValue);
            }
            return drawer.CreatePropertyGUI(property, typeKey, paramProp.stringValue);
        }
    }
}
