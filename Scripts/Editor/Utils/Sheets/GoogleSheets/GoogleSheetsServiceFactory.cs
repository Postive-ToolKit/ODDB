using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal static class GoogleSheetsServiceFactory
    {
        private const string ApplicationName = "ODDB Unity Editor";

        public static async Task<SheetsService> CreateAsync(CancellationToken ct = default)
        {
            var credential = await GoogleSheetsOAuthClient.CreateCredentialAsync(ct);
            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }
    }
}
