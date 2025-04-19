using TeamODD.ODDB.Runtime.Data.Enum;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    public class ODDBDataConverter
    {
        public object Convert(string value, ODDBDataType type)
        {
            return type switch
            {
                ODDBDataType.String => value,
                ODDBDataType.Int => int.TryParse(value, out var intValue) ? intValue : 0,
                ODDBDataType.Float => float.TryParse(value, out var floatValue) ? floatValue : 0f,
                ODDBDataType.Bool => bool.TryParse(value, out var boolValue) && boolValue,
                ODDBDataType.ScriptableObject => TryConvertReferenceObject<ScriptableObject>(value, out var scriptableObject) ? scriptableObject : null,
                ODDBDataType.Prefab => TryConvertReferenceObject<GameObject>(value, out var prefab) ? prefab : null,
                ODDBDataType.Sprite => TryConvertReferenceObject<Sprite>(value, out var sprite) ? sprite : null,
                _ => value
            };
        }

        private bool TryConvertReferenceObject<T>(string value, out T result) where T : Object
        {
            result = null;
            if (string.IsNullOrEmpty(value))
                return false;
            // Load the asset from the path in resources
            result = Resources.Load<T>(value);
            if(result == null) 
                return false;
            return true;
        }
    }
}