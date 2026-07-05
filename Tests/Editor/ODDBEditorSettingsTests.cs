using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Settings;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBEditorSettingsTests
    {
        private const string DefaultAssetPath = "Assets/Settings/ODDBEditorSettings.asset";
        private const string LegacyAssetPath = "Assets/Editor/ODDBEditorSettings.asset";
        private const string TestFolderPath = "Assets/Plugins/ODDB/Tests/Editor/Generated/ODDBEditorSettingsTests";
        private const string MovedAssetPath = TestFolderPath + "/MovedODDBEditorSettings.asset";
        private const string TestMarker = "ODDBEditorSettingsTests";

        [SetUp]
        public void SetUp()
        {
            DeleteTestGeneratedAssets();

            var existingSettings = FindSettingsAssetPaths();
            Assume.That(existingSettings, Is.Empty, "These tests require no pre-existing ODDBEditorSettings assets.");

            EnsureFolder(TestFolderPath);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestGeneratedAssets();
        }

        [Test]
        public void TryLoad_FindsSettingsAssetByTypeOutsideDefaultPath()
        {
            var movedSettings = ScriptableObject.CreateInstance<ODDBEditorSettings>();
            movedSettings.name = "MovedODDBEditorSettings";
            AssetDatabase.CreateAsset(movedSettings, MovedAssetPath);
            MarkAsTestAsset(movedSettings);

            var loaded = ODDBEditorSettings.TryLoad();

            Assert.That(loaded, Is.SameAs(movedSettings));
        }

        [Test]
        public void Setting_CreatesNewSettingsAssetInAssetsSettings()
        {
            var settings = ODDBEditorSettings.Setting;
            MarkAsTestAsset(settings);

            var assetPath = AssetDatabase.GetAssetPath(settings);

            Assert.That(assetPath, Is.EqualTo(DefaultAssetPath));
            Assert.That(AssetDatabase.LoadAssetAtPath<ODDBEditorSettings>(LegacyAssetPath), Is.Null);
        }

        private static void DeleteTestGeneratedAssets()
        {
            foreach (var path in FindSettingsAssetPaths())
            {
                var settings = AssetDatabase.LoadAssetAtPath<ODDBEditorSettings>(path);
                if (settings == null)
                    continue;

                if (path.StartsWith(TestFolderPath) || IsTestAsset(settings))
                    AssetDatabase.DeleteAsset(path);
            }

            if (AssetDatabase.IsValidFolder(TestFolderPath))
                AssetDatabase.DeleteAsset(TestFolderPath);
        }

        private static string[] FindSettingsAssetPaths()
        {
            return AssetDatabase.FindAssets($"t:{nameof(ODDBEditorSettings)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => AssetDatabase.LoadAssetAtPath<ODDBEditorSettings>(path) != null)
                .ToArray();
        }

        private static void EnsureFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void MarkAsTestAsset(ODDBEditorSettings settings)
        {
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("_googleSheetAPIURL").stringValue = TestMarker;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static bool IsTestAsset(ODDBEditorSettings settings)
        {
            var serialized = new SerializedObject(settings);
            return serialized.FindProperty("_googleSheetAPIURL").stringValue == TestMarker;
        }
    }
}
