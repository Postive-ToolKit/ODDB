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

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CellDrawer(ODDBDataType.Resources)]
    public class ResourceCellDrawer : ResourceSerializer, IODDBCellDrawer
    {
        public VisualElement CreatePropertyGUI(SerializedProperty property, ODDBDataType dataType, string param)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var referenceDataType = ODDBReferenceDataType.Object;
            if (Enum.TryParse(param, out ODDBReferenceDataType parsedType))
                referenceDataType = parsedType;
            var targetType = referenceDataType.GetReferenceDataBindType();
            var serializedData = targetField.stringValue;
            var value = Deserialize(serializedData, string.Empty)  as GameObject;

            var objectField = new ObjectField()
            {
                objectType = targetType,
                allowSceneObjects = false,
                value = value
            };

            objectField.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = Serialize(evt.newValue, string.Empty) ;
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return objectField;
        }
    }
}