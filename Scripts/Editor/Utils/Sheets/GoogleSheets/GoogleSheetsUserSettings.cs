using System;
using System.IO;
using System.Threading.Tasks;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal static class GoogleSheetsUserSettings
    {
        public static string TokenStorePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TeamODD",
            "ODDB",
            "GoogleSheets");

        public static string TokenFilePath => Path.Combine(TokenStorePath, "oauth-token.json");

        public static bool HasStoredAuthorization
        {
            get
            {
                try
                {
                    return File.Exists(TokenFilePath);
                }
                catch
                {
                    return false;
                }
            }
        }

        public static Task ClearAuthorizationAsync()
        {
            return Task.Run(ClearAuthorization);
        }

        public static void ClearAuthorization()
        {
            if (File.Exists(TokenFilePath))
                File.Delete(TokenFilePath);
        }
    }
}
