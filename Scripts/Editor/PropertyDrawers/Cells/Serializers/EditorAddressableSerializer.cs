#if ADDRESSABLE_EXIST
using TeamODD.ODDB.Runtime.Serializers;
using UnityEditor.AddressableAssets;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.PropertyDrawers.Serializers
{
    public class EditorAddressableSerializer : AddressableSerializer
    {
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

            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                Debug.LogError("Addressable Asset Settings not found.");
                return null;
            }

            // 모든 Entry를 순회하여 주소로 찾기
            foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
            {
                if (group == null)
                    continue;

                foreach (var entry in group.entries)
                {
                    if (entry.address == data)
                    {
                        // GUID로 에셋 경로 가져오기
                        var assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                        if (string.IsNullOrEmpty(assetPath))
                        {
                            Debug.LogWarning($"Asset path not found for GUID: {entry.guid}");
                            return null;
                        }

                        // 에셋 로드
                        return AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    }
                }
            }

            Debug.LogWarning($"Addressable entry not found for address: {data}");
            return null;
        }
    }
}
#endif