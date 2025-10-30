using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enum;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(FieldType))]
    public class FieldTypePropertyDrawer : PropertyDrawer
    {
        protected static List<ODDBDataType> CachedEnums
        {
            get
            {
                if (_cachedEnums != null) 
                    return _cachedEnums;
                _cachedEnums = new List<ODDBDataType>((ODDBDataType[])Enum.GetValues(typeof(ODDBDataType)));
                return _cachedEnums;
            }
        }
        private static List<ODDBDataType> _cachedEnums;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var typeProperty = property.FindPropertyRelative(nameof(FieldType.Type));
            var paramProperty = property.FindPropertyRelative(nameof(FieldType.Param));
            // ODDBDataType 모든 타입 나열
            EditorGUI.BeginProperty(position, label, property);
            var currentEnum = (ODDBDataType)typeProperty.enumValueFlag;

            var selections = new List<string>();
            var selectionEnumTarget = new Dictionary<string, ODDBDataType>();
            var paramMapping = new Dictionary<string, string>();
            var paramReverseMapping = new Dictionary<string, string>();
            foreach (var e in CachedEnums)
            {
                if (e.GetDataTypeOption().IsHideInSelector)
                    continue;
                
                var enumSelections = e.GetTypeSubSelector();
                if (enumSelections == null)
                {
                    selectionEnumTarget[e.ToString()] = e;
                    selections.Add(e.ToString());
                    continue;
                }

                foreach (var (realValue, selectionValue) in enumSelections.GetOptions())
                {
                    var selectionKey = $"{e.ToString()}/{selectionValue}";
                    
                    paramMapping[selectionKey] = realValue;
                    paramReverseMapping[realValue] = selectionKey;
                    
                    selectionEnumTarget[selectionKey] = e;
                    selections.Add(selectionKey);
                }
            }
            
            var param = paramProperty != null ? paramProperty.stringValue : string.Empty;
            if (paramReverseMapping.ContainsKey(param))
                param = paramReverseMapping[param];

            var path = currentEnum.ToString();
            if (!string.IsNullOrEmpty(param))
                path = param;
            
            var selectedIndex = selections.IndexOf(path);
            
            var newIndex = EditorGUI.Popup(position, label.text, selectedIndex, selections.ToArray());
            if (newIndex != selectedIndex)
            {
                var currentSelection = selections[newIndex];
                typeProperty.enumValueFlag = (int)selectionEnumTarget[currentSelection];
                
                // 해당 Enum을 가지고 있는 타입의 Param string 필드들을 찾아서 문자열 헐덩
                if (paramProperty != null && paramMapping.ContainsKey(currentSelection))
                    paramProperty.stringValue = paramMapping[currentSelection];
                else
                    paramProperty.stringValue = string.Empty;
            }
            
            EditorGUI.EndProperty();
            
                
        }
    }
}