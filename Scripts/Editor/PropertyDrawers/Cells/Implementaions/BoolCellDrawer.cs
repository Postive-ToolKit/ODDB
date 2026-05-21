using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEditor;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for boolean fields in the ODDB system.
    /// </summary>
    [CellDrawer("bool")]
    public class BoolCellDrawer : IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer = TypeRegistry.Get("bool") ?? new BoolSerializer();
        public VisualElement CreatePropertyGUI(SerializedProperty property, string typeKey, string param)
        {
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = (bool)(_serializer.Deserialize(serializedData, param) ?? false);

            var toggle = new Toggle()
            {
                value = value,
                style = { alignSelf = Align.Center}
            };

            toggle.RegisterValueChangedCallback(evt =>
            {
                var newSerializedData = _serializer.Serialize(evt.newValue, param);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return toggle;
        }
    }
}
