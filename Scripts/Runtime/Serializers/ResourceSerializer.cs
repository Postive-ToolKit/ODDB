using System;
using System.IO;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace TeamODD.ODDB.Runtime.Serializers
{
    public class ResourceSerializer : IDataSerializer
    {
        public string Serialize(object data, string param)
        {
#if UNITY_EDITOR
            if (data is not Object asset)
                return string.Empty;

            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;
            
            Debug.Log(assetPath);

            var resourcesIndex = assetPath.IndexOf("Resources/", StringComparison.Ordinal);
            if (resourcesIndex < 0)
                return string.Empty;

            var relativePath = assetPath.Substring(resourcesIndex + "Resources/".Length);
            return Path.ChangeExtension(relativePath, null);
#else
            if (data is Object obj)
                return obj.name;
            return string.Empty;
#endif
        }

        public object Deserialize(string serializedData, string param)
        {
            var oddbRefDataType = ODDBReferenceDataType.ScriptableObject;
            if (Enum.TryParse<ODDBReferenceDataType>(param, out var parsedType))
                oddbRefDataType = parsedType;
            var targetType = oddbRefDataType.GetReferenceDataBindType();
            if (TryConvertReferenceObject(serializedData,targetType, out var result))
                return result;
            return null;
        }
        
        private bool TryConvertReferenceObject(string value, Type refType, out Object result)
        {
            result = null;
            if (string.IsNullOrEmpty(value))
                return false;
            // Load the asset from the path in resources
            result = Resources.Load(value, refType);
            if(result == null) 
                return false;
            return true;
        }
    }
}