using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for ODDBCell with string data type
    /// </summary>
    [CellDrawer(ODDBDataType.View)]
    public class ViewCellDrawer : StringSerializer, IODDBCellDrawer
    {
        private const string NONE_OPTION = "None";
        private static IDataSerializer _serializer;
        public VisualElement CreatePropertyGUI(SerializedProperty property, ODDBDataType dataType, string param)
        {
            if (_serializer == null)
                _serializer = dataType.GetDataSerializer();
            
            var fieldTypeProp = property.FindPropertyRelative(Cell.DATA_TYPE_FIELD);
            var viewID = fieldTypeProp.FindPropertyRelative(FieldType.PARAM_FIELD).stringValue;
            
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = _serializer.Deserialize(serializedData, string.Empty)  as string;

            if (string.IsNullOrEmpty(viewID))
            {
                return new Label("No View Assigned");
            }

            var tables = ODDBEditorDI.Resolve<IODDBEditorUseCase>()
                .GetViews(view =>
                {
                    if (view is not Table table)
                        return false;
                    return table.ID == viewID || table.IsChildOf(viewID);
                })
                .Select(view => view as Table)
                .ToList();
            
            if (tables.Count <= 0)
            {
                return new Label("Invalid View");
            }
            
            var entityNames = new List<string>();
            entityNames.Add(NONE_OPTION);
            
            foreach (var table in tables)
                entityNames.AddRange(table.Rows.Select(row => row.ID.ToString()));

            if (entityNames.Count <= 0)
            {
                return new Label("No Entities in View");
            }
            
            var currentIndex = Mathf.Max(0, entityNames.IndexOf(value));
            
            var dropdownField = new DropdownField(entityNames, currentIndex);

            dropdownField.RegisterValueChangedCallback(evt =>
            {
                var newValue = evt.newValue;
                if (newValue == NONE_OPTION)
                    newValue = string.Empty;
                
                var newSerializedData = _serializer.Serialize(newValue, string.Empty) ;
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            });

            return dropdownField;
        }
    }
}