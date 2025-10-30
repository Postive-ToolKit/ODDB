﻿using TeamODD.ODDB.Editors.Attributes;
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
    /// <summary>
    /// Property drawer for float data type.
    /// </summary>
    [CellDrawer(ODDBDataType.Float)]
    public class FloatCellDrawer : FloatSerializer, IODDBCellDrawer
    {
        public VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = Deserialize(serializedData) is float floatValue ? floatValue : 0f;

            var floatField = new FloatField()
            {
                value = value
            };

            floatField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = Serialize(evt.newValue);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return floatField;
        }
    }
}