﻿using System;
 using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CellDrawer(ODDBDataType.Resources)]
    public class ResourceCellDrawer : IODDBCellDrawer
    {
        private static IDataSerializer _serializer;
        public VisualElement CreatePropertyGUI(SerializedProperty property, ODDBDataType dataType, string param)
        {
            if (_serializer == null)
                _serializer = dataType.GetDataSerializer();
            
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var referenceDataType = ODDBReferenceDataType.Object;
            if (Enum.TryParse(param, out ODDBReferenceDataType parsedType))
                referenceDataType = parsedType;
            var targetType = referenceDataType.GetReferenceDataBindType();
            var serializedData = targetField.stringValue;
            var value = _serializer.Deserialize(serializedData, param) as Object;

            var objectField = new ObjectField()
            {
                objectType = targetType,
                allowSceneObjects = false,
                value = value
            };

            objectField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = _serializer.Serialize(evt.newValue, param) ;
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return objectField;
        }
    }
}