using System;
using System.IO;
using System.Text;
using TeamODD.ODDB.Scripts.Runtime.Data;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace TeamODD.ODDB.Editors.UI.Fields.References
{
    public abstract class ODDBReferenceField<T> : IODDBField where T : UnityEngine.Object
    {
        private const string ASSET_BASE_PATH = "Assets/Resources/";
        public VisualElement Root => _root;
        private readonly VisualElement _root;
        private readonly ObjectField _objectField;
        private string _currentPath;
        private Action<object> _onValueChanged;
        
        protected ODDBReferenceField()
        {
            _root = new VisualElement();
            _root.style.flexDirection = FlexDirection.Row;
            _root.style.flexGrow = 1;
            _objectField = new ObjectField()
            {
                objectType = typeof(T),
                allowSceneObjects = false,
                style = { flexGrow = 1 }
            };
            _root.Add(_objectField);
            
            // 필드가 파괴될 때 감시 해제
            _root.RegisterCallback<DetachFromPanelEvent>(_ => UnregisterWatcher());
        }

        public void SetValue(object value)
        {
            UnregisterWatcher();
            
            if (value == null)
            {
                _objectField.value = null;
                return;
            }

            var dataPath = ODDBDataConverter<string>.TryConvert(value);
            Debug.Log("Reference data is path : " + dataPath);
            if (TryGetValue(dataPath, out var convertedValue)) 
            {
                _currentPath = dataPath;
                _objectField.value = convertedValue;

                // ping convertedValue
                EditorGUIUtility.PingObject(convertedValue);
                // 에셋과 경로 모두 감시 시작
                RegisterWatcher(convertedValue, dataPath);
                return;
            }
            
            Debug.LogWarning("Failed to find the asset at resources : " + dataPath);
            _currentPath = string.Empty;
            _objectField.value = null;
        }

        private void RegisterWatcher(T asset, string path)
        {
            var fullPath = ASSET_BASE_PATH + path;
            ODDBAssetPathWatcher.WatchAsset(asset, OnAssetPathChanged);
            ODDBAssetPathWatcher.WatchPath(fullPath, OnAssetPathChanged);
        }

        private void UnregisterWatcher()
        {
            if (_objectField.value != null)
            {
                ODDBAssetPathWatcher.StopWatchingAsset(_objectField.value, OnAssetPathChanged);
            }
            if (!string.IsNullOrEmpty(_currentPath))
            {
                var fullPath = ASSET_BASE_PATH + _currentPath;
                ODDBAssetPathWatcher.StopWatchingPath(fullPath, OnAssetPathChanged);
            }
        }

        private void OnAssetPathChanged(string newPath)
        {
            if (string.IsNullOrEmpty(newPath)) return;

            // Resources 폴더 상대 경로 추출
            const string resourcesPath = "/Resources/";
            int resourcesIndex = newPath.IndexOf(resourcesPath);
            if (resourcesIndex == -1) return;

            string relativePath = newPath.Substring(resourcesIndex + resourcesPath.Length);
            relativePath = Path.ChangeExtension(relativePath, null);

            _currentPath = relativePath;
            _onValueChanged?.Invoke(GetValue());
        }

        private bool TryGetValue(string path, out T value)
        {
            value = null;
            if (string.IsNullOrEmpty(path))
                return false;
            // Load the asset from the path in resources
            value = Resources.Load<T>(path);
            if(value == null) 
                return false;
            return true;
        }

        public object GetValue()
        {
            return _currentPath;
        }

        public void RegisterValueChangedCallback(Action<object> callback)
        {
            _onValueChanged = callback;
            _objectField.RegisterValueChangedCallback(evt =>
            {
                UnregisterWatcher();
                
                var curObject = evt.newValue as T;
                if (curObject == null) 
                {
                    _currentPath = string.Empty;
                }
                else 
                {
                    var assetPath = AssetDatabase.GetAssetPath(curObject);
                    if (string.IsNullOrEmpty(assetPath))
                        return;
                    
                    var nameSplits = assetPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    var sb = new StringBuilder();
                    
                    for (int i = nameSplits.Length - 1; i >= 0; i--)
                    {
                        if (nameSplits[i] == "Resources")
                            break;
                        sb.Insert(0, nameSplits[i]);
                        sb.Insert(0, '/');
                    }
                    
                    if (sb.Length > 0)
                        sb.Remove(0, 1);
                    assetPath = sb.ToString();
                    
                    assetPath = Path.ChangeExtension(assetPath, null);
                    _currentPath = assetPath;
                    
                    RegisterWatcher(curObject, _currentPath);
                }
                
                callback?.Invoke(GetValue());
            });
        }
    }
}