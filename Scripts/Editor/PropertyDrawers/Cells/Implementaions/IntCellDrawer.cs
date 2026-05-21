﻿using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for ODDBCell of type Int.
    /// </summary>
    [CellDrawer("int")]
    public class IntCellDrawer : IntSerializer, IODDBCellDrawer
    {
        private static IDataSerializer _serializer = ODDBDataType.Int.GetDataSerializer();
        public VisualElement CreatePropertyGUI(SerializedProperty property, string typeKey, string param)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = (int)_serializer.Deserialize(serializedData, param) ;

            var intField = new IntegerField()
            {
                value = value
            };

            intField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = _serializer.Serialize(evt.newValue, param) ;
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return intField;
        }
    }
}