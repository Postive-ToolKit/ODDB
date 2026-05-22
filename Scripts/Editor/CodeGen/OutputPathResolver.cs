using System.Collections.Generic;
using System.IO;
using TeamODD.ODDB.Editors.Settings;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.CodeGen
{
    /// <summary>
    /// Centralizes filesystem decisions for the code generator:
    /// - Resolves the configured output folder and validates it exists.
    /// - Lists generator-owned .cs files via the GeneratedFileMarker header.
    /// - Identifies stale generator files for cleanup.
    /// </summary>
    internal static class OutputPathResolver
    {
        public static bool TryGetValidOutputFolder(out string absoluteFolder, out string failureReason)
        {
            absoluteFolder = null;
            var settings = ODDBEditorSettings.Setting;
            var rel = settings != null ? settings.GeneratedCodePath : null;
            if (string.IsNullOrEmpty(rel))
            {
                failureReason = "ODDBEditorSettings.GeneratedCodePath is empty. Set it before generating.";
                return false;
            }
            // Settings stores Assets-relative-ish path; canonicalize.
            string assetsRel = rel.Replace('\\', '/').TrimStart('/');
            if (!assetsRel.StartsWith("Assets/"))
                assetsRel = "Assets/" + assetsRel;

            string abs = Path.GetFullPath(assetsRel);
            if (!Directory.Exists(abs))
            {
                failureReason = $"Generated code folder does not exist: {assetsRel}";
                return false;
            }
            absoluteFolder = abs;
            failureReason = null;
            return true;
        }

        public static IEnumerable<string> EnumerateGeneratedFiles(string absoluteFolder)
        {
            if (!Directory.Exists(absoluteFolder))
                yield break;
            foreach (var path in Directory.EnumerateFiles(absoluteFolder, "*.cs", SearchOption.TopDirectoryOnly))
            {
                if (GeneratedFileMarker.IsGenerated(path))
                    yield return path;
            }
        }

        /// <summary>
        /// Returns full paths to generator-owned .cs files in the output folder
        /// whose class name (file stem) is NOT in <paramref name="keepClassNames"/>.
        /// </summary>
        public static IEnumerable<string> FindStaleFiles(string absoluteFolder, ISet<string> keepClassNames)
        {
            foreach (var path in EnumerateGeneratedFiles(absoluteFolder))
            {
                var stem = Path.GetFileNameWithoutExtension(path);
                if (!keepClassNames.Contains(stem))
                    yield return path;
            }
        }

        public static void DeleteFileWithMeta(string absoluteFile)
        {
            if (File.Exists(absoluteFile)) File.Delete(absoluteFile);
            var meta = absoluteFile + ".meta";
            if (File.Exists(meta)) File.Delete(meta);
        }

        /// <summary>Convert absolute path back to Assets-relative path for AssetDatabase calls.</summary>
        public static string ToAssetsRelative(string absolutePath)
        {
            string norm = absolutePath.Replace('\\', '/');
            string data = Application.dataPath.Replace('\\', '/');
            if (norm.StartsWith(data))
                return "Assets" + norm.Substring(data.Length);
            return norm;
        }
    }
}
