#if ADDRESSABLE_EXIST
using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TeamODD.ODDB.Editors.PropertyDrawers.Serializers
{
    public class EditorAddressableSerializer : AddressableSerializer
    {
        private static AddressableAssetSettings _cachedSettings;
        private static Hash128 _cachedSettingsHash;
        private static readonly Dictionary<string, string> _guidByAddress = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Object> _assetByAddress = new(StringComparer.Ordinal);
        private static readonly HashSet<string> _missingAddresses = new(StringComparer.Ordinal);

        public override string Serialize(object data, string param)
        {
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                Debug.LogError("Addressable Asset Settings not found.");
                return string.Empty;
            }
            
            if (data is not Object asset)
                return string.Empty;

            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;
                    
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("Could not find GUID for asset.");
                return string.Empty;
            }

            // 4. Addressable 설정에서 GUID로 Entry(항목)를 찾습니다.
            var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
            if (entry != null)
                return entry.address;
            
            Debug.LogWarning("Asset is not marked as Addressable.");
            return string.Empty;
        }
        public override object Deserialize(string data, string param)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Asset Settings not found.");
                return null;
            }

            EnsureLookup(settings);
            if (_assetByAddress.TryGetValue(data, out var cachedAsset) && cachedAsset != null)
                return cachedAsset;

            if (!_guidByAddress.TryGetValue(data, out var guid))
            {
                WarnMissingOnce(data, $"Addressable entry not found for address: {data}");
                return null;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                WarnMissingOnce(data, $"Asset path not found for GUID: {guid}");
                return null;
            }

            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset != null)
                _assetByAddress[data] = asset;
            return asset;
        }

        private static void EnsureLookup(AddressableAssetSettings settings)
        {
            var hash = settings.currentHash;
            if (ReferenceEquals(_cachedSettings, settings) && _cachedSettingsHash == hash)
                return;

            _cachedSettings = settings;
            _cachedSettingsHash = hash;
            _guidByAddress.Clear();
            _assetByAddress.Clear();
            _missingAddresses.Clear();

            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                foreach (var entry in group.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.address)) continue;
                    if (!_guidByAddress.ContainsKey(entry.address))
                        _guidByAddress.Add(entry.address, entry.guid);
                }
            }
        }

        private static void WarnMissingOnce(string address, string message)
        {
            if (_missingAddresses.Add(address))
                Debug.LogWarning(message);
        }
    }
}
#endif
