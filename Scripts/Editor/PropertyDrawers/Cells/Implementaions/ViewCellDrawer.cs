using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enum;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Property drawer for ODDBCell with string data type
    /// </summary>
    [CellDrawer(ODDBDataType.View)]
    public class ViewCellDrawer : StringSerializer, IODDBCellDrawer
    {
        private const string NONE_OPTION = "None";
        public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var fieldTypeProp = property.FindPropertyRelative(Cell.DATA_TYPE_FIELD);
            var viewID = fieldTypeProp.FindPropertyRelative(FieldType.PARAM_FIELD).stringValue;
            
            var targetField = property.FindPropertyRelative(Cell.SERIALIZED_DATA_FIELD);
            var serializedData = targetField.stringValue;
            var value = Deserialize(serializedData) as string;
            // GUI 그리기
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            
            if (string.IsNullOrEmpty(viewID))
            {
                EditorGUI.LabelField(position, "No View Assigned");
                EditorGUI.EndProperty();
                return;
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
                EditorGUI.LabelField(position, "Invalid View");
                EditorGUI.EndProperty();
                return;
            }
            
            var entityNames = new List<string>();
            entityNames.Add(NONE_OPTION);
            
            foreach (var table in tables)
                entityNames.AddRange(table.Rows.Select(row => row.ID.ToString()));

            if (entityNames.Count <= 0)
            {
                EditorGUI.LabelField(position, "No Entities in View");
                EditorGUI.EndProperty();
                return;
            }
            
            var currentIndex = Mathf.Max(0, entityNames.IndexOf(value));
            var newIndex = EditorGUI.Popup(position, label.text, currentIndex, entityNames.ToArray());
            var newValue = entityNames[newIndex];

            if (EditorGUI.EndChangeCheck())
            {
                if (newValue == NONE_OPTION)
                    newValue = string.Empty;
                
                var newSerializedData = Serialize(newValue);
                targetField.stringValue = newSerializedData;
                property.serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUI.EndProperty();
        }
    }
}