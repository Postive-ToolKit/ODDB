using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enum;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CellDrawer(ODDBDataType.String)]
    public class StringCellDrawer : StringSerializer, IODDBCellDrawer
    {
        public VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = Deserialize(serializedData) as string;

            var textField = new TextField()
            {
                value = value ?? string.Empty
            };

            textField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = Serialize(evt.newValue);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return textField;
        }
    }
}