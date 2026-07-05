using System.IO;
using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors.CodeGen;
using TeamODD.ODDB.Editors.Settings;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBEditorSettingsTests
    {
        private const string DefaultAssetPath = "Assets/Settings/ODDBEditorSettings.asset";
        private const string LegacyAssetPath = "Assets/Editor/ODDBEditorSettings.asset";
        private const string GeneratedRootPath = "Assets/Plugins/ODDB/Tests/Editor/Generated";
        private const string TestFolderPath = GeneratedRootPath + "/ODDBEditorSettingsTests";
        private const string MovedAssetPath = TestFolderPath + "/MovedODDBEditorSettings.asset";
        private const string GeneratedCodeFolderPath = TestFolderPath + "/GeneratedCode";
        private const string RuntimeSettingsAssetPath = "Assets/Resources/ODDBRuntimeSettings.asset";
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

        [Test]
        public void OutputPathResolver_AcceptsAbsoluteGeneratedCodePathInsideAssets()
        {
            EnsureFolder(GeneratedCodeFolderPath);

            var settings = ODDBEditorSettings.Setting;
            MarkAsTestAsset(settings);
            var absolutePath = Path.GetFullPath(GeneratedCodeFolderPath).Replace('\\', '/');
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("_generatedCodePath").stringValue = absolutePath;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var resolved = OutputPathResolver.TryGetValidOutputFolder(out var absoluteFolder, out var failureReason);

            Assert.That(resolved, Is.True, failureReason);
            Assert.That(absoluteFolder.Replace('\\', '/'), Is.EqualTo(absolutePath));
        }

        [Test]
        public void PathUtility_ConvertsProjectFolderToAssetsRelativePath()
        {
            var absolutePath = Path.GetFullPath(GeneratedCodeFolderPath).Replace('\\', '/');

            var storedPath = ODDBPathUtility.ToProjectRelativePath(absolutePath);

            Assert.That(storedPath, Is.EqualTo(GeneratedCodeFolderPath));
        }

        [Test]
        public void RuntimeSettings_PathSetterAcceptsAssetsRelativePath()
        {
            var settings = ScriptableObject.CreateInstance<ODDBRuntimeSettings>();

            settings.Path = "Assets/Resources";

            Assert.That(settings.DBPath, Is.EqualTo("/Resources"));
            Assert.That(settings.Path.Replace('\\', '/'), Is.EqualTo((Application.dataPath + "/Resources").Replace('\\', '/')));
        }

        [Test]
        public void RuntimeSettings_TryLoadUsesResourcesRuntimeSettingsAsset()
        {
            var created = false;
            var runtimeSettings = AssetDatabase.LoadAssetAtPath<ODDBRuntimeSettings>(RuntimeSettingsAssetPath);
            if (runtimeSettings == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                runtimeSettings = ScriptableObject.CreateInstance<ODDBRuntimeSettings>();
                runtimeSettings.name = "ODDBRuntimeSettings";
                runtimeSettings.Path = "Assets/Resources";
                AssetDatabase.CreateAsset(runtimeSettings, RuntimeSettingsAssetPath);
                AssetDatabase.SaveAssets();
                created = true;
            }

            try
            {
                var loaded = ODDBRuntimeSettings.TryLoad();
                var resolvedPath = ODDBRuntimeSettings.ResolveDatabasePath();

                Assert.That(loaded, Is.SameAs(runtimeSettings));
                Assert.That(resolvedPath.Replace('\\', '/'), Is.EqualTo(runtimeSettings.FullDBPath.Replace('\\', '/')));
            }
            finally
            {
                if (created)
                    AssetDatabase.DeleteAsset(RuntimeSettingsAssetPath);
            }
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

            if (AssetDatabase.IsValidFolder(GeneratedRootPath) && IsAssetFolderEmpty(GeneratedRootPath))
                AssetDatabase.DeleteAsset(GeneratedRootPath);
        }

        private static string[] FindSettingsAssetPaths()
        {
            return AssetDatabase.FindAssets($"t:{nameof(ODDBEditorSettings)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => AssetDatabase.LoadAssetAtPath<ODDBEditorSettings>(path) != null)
                .ToArray();
        }

        private static bool IsAssetFolderEmpty(string folderPath)
        {
            var absolutePath = Path.GetFullPath(folderPath);
            return Directory.Exists(absolutePath) && !Directory.EnumerateFileSystemEntries(absolutePath).Any();
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
