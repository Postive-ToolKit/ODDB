using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using Object = UnityEngine.Object;

namespace TeamODD.ODDB.Editors.UI.Fields
{
    public class ODDBPrefabField : IODDBField
    {
        private readonly VisualElement _root;
        private readonly ObjectField _objectField;
        private readonly Button _locateButton;
        private string _currentPath;

        public VisualElement Root => _root;

        public ODDBPrefabField()
        {
            _root = new VisualElement();
            _root.style.flexDirection = FlexDirection.Row;
            _root.style.flexGrow = 1;

            _objectField = new ObjectField()
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
                style = { flexGrow = 1 }
            };

            _locateButton = new Button(() => LocatePrefab())
            {
                text = "Locate",
                style = { width = 50 }
            };

            _root.Add(_objectField);
            _root.Add(_locateButton);
        }

        public void SetValue(object value)
        {
            if (value == null)
            {
                _objectField.value = null;
                _currentPath = null;
                return;
            }

            _currentPath = value.ToString();
            if (!string.IsNullOrEmpty(_currentPath))
            {
                var prefab = Resources.Load<GameObject>(_currentPath);
                _objectField.value = prefab;
            }
        }

        public object GetValue()
        {
            var obj = _objectField.value as GameObject;
            if (obj == null) return null;

            // 리소스 경로 추출
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath)) return null;

            // Resources 폴더로부터의 상대 경로 추출
            const string resourcesPath = "/Resources/";
            int resourcesIndex = assetPath.IndexOf(resourcesPath);
            if (resourcesIndex == -1) 
            {
                Debug.LogWarning("Prefab must be in a Resources folder");
                return null;
            }

            string relativePath = assetPath.Substring(resourcesIndex + resourcesPath.Length);
            // .prefab 확장자 제거
            relativePath = System.IO.Path.ChangeExtension(relativePath, null);
            return relativePath;
        }

        public void RegisterValueChangedCallback(Action<object> callback)
        {
            _objectField.RegisterValueChangedCallback(evt =>
            {
                callback?.Invoke(GetValue());
            });
        }

        private void LocatePrefab()
        {
            if (string.IsNullOrEmpty(_currentPath)) return;
            
            var prefab = Resources.Load<GameObject>(_currentPath);
            if (prefab != null)
            {
                var assetPath = UnityEditor.AssetDatabase.GetAssetPath(prefab);
                UnityEditor.EditorGUIUtility.PingObject(prefab);
                UnityEditor.Selection.activeObject = prefab;
            }
        }
    }
} 