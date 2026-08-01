using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Newtonsoft.Json.Linq;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal static class GoogleSheetsServiceFactory
    {
        private const string ApplicationName = "ODDB Unity Editor";

        public static SheetsService Create()
        {
            if (!GoogleSheetsUserSettings.TryGetCredentialPath(out var path, out var failureReason))
                throw new InvalidOperationException(failureReason);

            ValidateCredentialFile(path);
            var credential = CredentialFactory.FromFile<ServiceAccountCredential>(path)
                .ToGoogleCredential()
                .CreateScoped(SheetsService.Scope.Spreadsheets);

            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        public static async Task<string> ReadServiceAccountEmailAsync(CancellationToken ct = default)
        {
            if (!GoogleSheetsUserSettings.TryGetCredentialPath(out var path, out var failureReason))
                throw new InvalidOperationException(failureReason);

            var json = await Task.Run(() => File.ReadAllText(path), ct);
            var root = JObject.Parse(json);
            ValidateCredentialJson(root);
            return root.Value<string>("client_email");
        }

        private static void ValidateCredentialFile(string path)
        {
            JObject root;
            try
            {
                root = JObject.Parse(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("The Google credential file is not valid JSON.", e);
            }

            ValidateCredentialJson(root);
        }

        private static void ValidateCredentialJson(JObject root)
        {
            if (!string.Equals(root.Value<string>("type"), "service_account", StringComparison.Ordinal))
                throw new InvalidOperationException("The selected Google credential must be a service-account JSON file.");
            if (string.IsNullOrWhiteSpace(root.Value<string>("client_email"))
                || string.IsNullOrWhiteSpace(root.Value<string>("private_key")))
            {
                throw new InvalidOperationException("The service-account JSON is missing client_email or private_key.");
            }
        }
    }
}
