using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enum;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for boolean fields in the ODDB system.
    /// </summary>
    [CellDrawer(ODDBDataType.Bool)]
    public class BoolCellDrawer : BoolSerializer, IODDBCellDrawer
    {
        public VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = (bool)(Deserialize(serializedData) ?? false);

            var toggle = new Toggle()
            {
                value = value,
                style = { alignSelf = Align.Center}
            };

            toggle.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = Serialize(evt.newValue);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return toggle;
        }
    }
}