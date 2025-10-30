﻿using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enum;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for ODDBCell of type Int.
    /// </summary>
    [CellDrawer(ODDBDataType.Int)]
    public class IntCellDrawer : IntSerializer, IODDBCellDrawer
    {
        public VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = (int)Deserialize(serializedData);

            var intField = new IntegerField()
            {
                value = value
            };

            intField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = Serialize(evt.newValue);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return intField;
        }
    }
}