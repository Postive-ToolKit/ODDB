using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI.Fields.References
{
    public class ODDBAssetPathWatcher : AssetPostprocessor
    {
        private static readonly Dictionary<string, List<Action<string>>> _pathWatchers = new();
        private static readonly Dictionary<UnityEngine.Object, List<Action<string>>> _assetWatchers = new();

        public static void WatchPath(string originalPath, Action<string> onPathChanged)
        {
            if (!_pathWatchers.ContainsKey(originalPath))
            {
                _pathWatchers[originalPath] = new List<Action<string>>();
            }
            _pathWatchers[originalPath].Add(onPathChanged);
        }

        public static void WatchAsset(UnityEngine.Object asset, Action<string> onPathChanged)
        {
            if (!_assetWatchers.ContainsKey(asset))
            {
                _assetWatchers[asset] = new List<Action<string>>();
            }
            _assetWatchers[asset].Add(onPathChanged);
        }

        public static void StopWatchingPath(string originalPath, Action<string> callback)
        {
            if (_pathWatchers.TryGetValue(originalPath, out var callbacks))
            {
                callbacks.Remove(callback);
                if (callbacks.Count == 0)
                {
                    _pathWatchers.Remove(originalPath);
                }
            }
        }

        public static void StopWatchingAsset(UnityEngine.Object asset, Action<string> callback)
        {
            if (_assetWatchers.TryGetValue(asset, out var callbacks))
            {
                callbacks.Remove(callback);
                if (callbacks.Count == 0)
                {
                    _assetWatchers.Remove(asset);
                }
            }
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (var i = 0; i < movedAssets.Length; i++)
            {
                var oldPath = movedFromAssetPaths[i];
                var newPath = movedAssets[i];

                // 경로 기반 감시자들에게 알림
                if (_pathWatchers.TryGetValue(oldPath, out var pathCallbacks))
                {
                    foreach (var callback in pathCallbacks)
                    {
                        callback?.Invoke(newPath);
                    }
                }

                // 에셋 기반 감시자들에게 알림
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newPath);
                if (asset != null && _assetWatchers.TryGetValue(asset, out var assetCallbacks))
                {
                    foreach (var callback in assetCallbacks)
                    {
                        callback?.Invoke(newPath);
                    }
                }
            }
        }
    }
} 