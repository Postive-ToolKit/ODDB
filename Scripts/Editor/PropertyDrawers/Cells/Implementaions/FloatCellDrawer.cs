﻿using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
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
    public class FloatCellDrawer : IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer = ODDBDataType.Float.GetDataSerializer();
        public VisualElement CreatePropertyGUI(SerializedProperty property, ODDBDataType dataType, string param)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = _serializer.Deserialize(serializedData, param) is float floatValue ? floatValue : 0f;

            var floatField = new FloatField()
            {
                value = value
            };

            floatField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = _serializer.Serialize(evt.newValue, param) ;
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return floatField;
        }
    }
}