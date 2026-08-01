using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal static class GoogleSheetsUserSettings
    {
        private const string CredentialPathSuffix = ".GoogleSheets.ServiceAccountCredentialPath";

        public static string CredentialPath
        {
            get => EditorPrefs.GetString(GetProjectKey(CredentialPathSuffix), string.Empty);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    EditorPrefs.DeleteKey(GetProjectKey(CredentialPathSuffix));
                    return;
                }

                EditorPrefs.SetString(
                    GetProjectKey(CredentialPathSuffix),
                    Path.GetFullPath(value.Trim()));
            }
        }

        public static bool TryGetCredentialPath(out string path, out string failureReason)
        {
            path = CredentialPath;
            if (string.IsNullOrEmpty(path))
            {
                failureReason = "Select a Google service-account JSON file from ODDB > Google Sheets > Setup.";
                return false;
            }

            if (!File.Exists(path))
            {
                failureReason = $"The configured Google service-account JSON file does not exist: {path}";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private static string GetProjectKey(string suffix)
        {
            var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                .Replace('\\', '/')
                .ToLowerInvariant();
            return $"TeamODD.ODDB.{Hash128.Compute(projectPath)}{suffix}";
        }
    }
}
