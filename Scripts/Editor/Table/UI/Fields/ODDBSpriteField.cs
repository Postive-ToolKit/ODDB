using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace TeamODD.ODDB.Editors.UI.Fields
{
    public class ODDBSpriteField : IODDBField
    {
        private readonly VisualElement _root;
        private readonly ObjectField _objectField;
        private readonly Button _locateButton;
        private string _currentPath;

        public VisualElement Root => _root;

        public ODDBSpriteField()
        {
            _root = new VisualElement();
            _root.style.flexDirection = FlexDirection.Row;
            _root.style.flexGrow = 1;

            _objectField = new ObjectField()
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                style = { flexGrow = 1 }
            };

            _locateButton = new Button(() => LocateSprite())
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
                var sprite = Resources.Load<Sprite>(_currentPath);
                _objectField.value = sprite;
            }
        }

        public object GetValue()
        {
            var obj = _objectField.value as Sprite;
            if (obj == null) return null;

            // 리소스 경로 추출
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath)) return null;

            // Resources 폴더로부터의 상대 경로 추출
            const string resourcesPath = "/Resources/";
            int resourcesIndex = assetPath.IndexOf(resourcesPath);
            if (resourcesIndex == -1) 
            {
                Debug.LogWarning("Sprite must be in a Resources folder");
                return null;
            }

            string relativePath = assetPath.Substring(resourcesIndex + resourcesPath.Length);
            // 확장자 제거
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

        private void LocateSprite()
        {
            if (string.IsNullOrEmpty(_currentPath)) return;
            
            var sprite = Resources.Load<Sprite>(_currentPath);
            if (sprite != null)
            {
                UnityEditor.EditorGUIUtility.PingObject(sprite);
                UnityEditor.Selection.activeObject = sprite;
            }
        }
    }
} 