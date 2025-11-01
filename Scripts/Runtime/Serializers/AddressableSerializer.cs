#if ADDRESSABLE_EXIST
using System;
using System.Collections.Generic;
using System.Reflection;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace TeamODD.ODDB.Runtime.Serializers
{
    public class AddressableSerializer : IDataSerializer
    {
        private const string LOAD_ASSET_ASYNC_METHOD = nameof(Addressables.LoadAssetAsync);
        private const string WAIT_FOR_COMPLETION_METHOD = nameof(AsyncOperationHandle.WaitForCompletion);
        private const string RESULT_PROPERTY = nameof(AsyncOperationHandle<Object>.Result);
        
        private static readonly Dictionary<Type, MethodInfo> _cachedLoadMethods = new Dictionary<Type, MethodInfo>();
        
        public virtual string Serialize(object data, string param)
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"{nameof(AddressableSerializer)}.{nameof(Serialize)} runtime serialization is not supported.");
            #endif
            return string.Empty;
        }

        public virtual object Deserialize(string serializedData, string param)
        {
            // Need to make how to handle async properly later
            // TODO : Make setting to always wait for completion or get handle and let user handle it
            if (string.IsNullOrEmpty(serializedData))
                return null;
            
            var oddbRefDataType = ODDBReferenceDataType.Object;
            if (Enum.TryParse<ODDBReferenceDataType>(param, out var parsedType))
                oddbRefDataType = parsedType;
            
            var assetType = oddbRefDataType.GetReferenceDataBindType();
            
            if (assetType == null)
                assetType = typeof(Object);
            
            try
            {
                var loadMethod = GetOrCreateLoadMethod(assetType);
                var handle = loadMethod.Invoke(null, new object[] { serializedData });
                
                var waitMethod = handle.GetType().GetMethod(WAIT_FOR_COMPLETION_METHOD);
                waitMethod?.Invoke(handle, null);
                
                var resultProperty = handle.GetType().GetProperty(RESULT_PROPERTY);
                return resultProperty?.GetValue(handle);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to load addressable asset: " + serializedData + " | Error: " + e.Message);
                return null;
            }
        }
        
        private static MethodInfo GetOrCreateLoadMethod(Type assetType)
        {
            if (_cachedLoadMethods.TryGetValue(assetType, out var cachedMethod))
                return cachedMethod;
            
            var genericMethod = typeof(Addressables)
                .GetMethod(LOAD_ASSET_ASYNC_METHOD, BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(object) }, null);
            
            if (genericMethod == null)
            {
                Debug.LogError($"Failed to find {LOAD_ASSET_ASYNC_METHOD} method");
                return null;
            }
            
            var typedMethod = genericMethod.MakeGenericMethod(assetType);
            _cachedLoadMethods[assetType] = typedMethod;
            
            return typedMethod;
        }
    }
}
#endif