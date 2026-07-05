using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace TeamODD.ODDB.Editors.Utils
{
    public class ODDBPathUtility
    {
        public string GetPath(string mustContain = null, string basePath = null)
        {
            mustContain = NormalizeSlashes(mustContain ?? string.Empty);
            basePath = ToAbsoluteProjectPath(basePath ?? Application.dataPath);

            string result;
            do{
                result = EditorUtility.OpenFolderPanel("Set Path of Database File", basePath, "");
                if (string.IsNullOrEmpty(result))
                {
                    EditorUtility.DisplayDialog("Canceled", "The path selection was canceled.", "OK");
                    return null;
                }

                result = NormalizeSlashes(result);
                
                if(!string.IsNullOrEmpty(mustContain) && !result.Contains(mustContain))
                {
                    //Show error dialog
                    var sb = new StringBuilder();
                    sb.AppendLine("The selected path is not a valid database file path.");
                    sb.AppendLine($"Please select a path in the {mustContain} folder.");
                    EditorUtility.DisplayDialog("Error", sb.ToString(), "OK");
                }
                //check is the derectory in the project resources folder
            } while(!string.IsNullOrEmpty(mustContain) && !result.Contains(mustContain));
            return ToProjectRelativePath(result);
        }

        public static string ToProjectRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            var normalized = NormalizeSlashes(path).TrimEnd('/');
            var dataPath = NormalizeSlashes(Application.dataPath).TrimEnd('/');

            if (IsSameOrChildPath(normalized, dataPath))
                return "Assets" + normalized.Substring(dataPath.Length);

            return normalized;
        }

        private static string ToAbsoluteProjectPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Application.dataPath;

            var normalized = NormalizeSlashes(path);
            if (Path.IsPathRooted(normalized))
                return normalized;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return NormalizeSlashes(Path.GetFullPath(Path.Combine(projectRoot ?? Directory.GetCurrentDirectory(), normalized)));
        }

        private static string NormalizeSlashes(string path)
        {
            return string.IsNullOrEmpty(path) ? path : path.Replace("\\", "/");
        }

        private static bool IsSameOrChildPath(string path, string parentPath)
        {
            return string.Equals(path, parentPath, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(parentPath + "/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
