using System;
using System.Linq;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEditor;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for Enum data type.
    /// </summary>
    [CellDrawer("enum")]
    public class EnumCellDrawer : IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("enum") ?? new EnumSerializer();
        public VisualElement CreatePropertyGUI(SerializedProperty property, string typeKey, string param)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = _serializer.Deserialize(serializedData, param) as Enum;
            if (value == null)
            {
                var types = ODDBEnumUtility.GetEnumValues(param);
                if (types != null)
                {
                    value = types.Values.FirstOrDefault();
                    targetField.stringValue = _serializer.Serialize(value, param);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            var field = new EnumField()
            {
                value = value
            };
            field.Init(value);
            field.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = _serializer.Serialize(evt.newValue, param);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }
    }
}
