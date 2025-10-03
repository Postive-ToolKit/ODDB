using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Enum;
using UnityEditor;
using UnityEngine;
namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ODDBCell))]
    public class ODDBCellPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var dataType = (ODDBDataType)property.FindPropertyRelative(ODDBCell.DATA_TYPE_FIELD).enumValueFlag;
            dataType.GetCellDrawer().OnGUI(position, property, label);
        }
    }
}