using System;
using System.Net;
using Google;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal static class GoogleSheetsApiError
    {
        public static string Describe(GoogleApiException exception)
        {
            if (exception == null)
                return "Google Sheets request failed.";

            switch (exception.HttpStatusCode)
            {
                case HttpStatusCode.Forbidden:
                    return "Google Sheets access was denied. Enable Google Sheets API and share the spreadsheet with the configured service-account email as an Editor.";
                case HttpStatusCode.NotFound:
                    return "The configured Google spreadsheet was not found. Check the Spreadsheet ID and service-account sharing permissions.";
                case HttpStatusCode.Unauthorized:
                    return "Google service-account authentication failed. Check or replace the credential JSON file.";
                case (HttpStatusCode)429:
                    return "Google Sheets API quota was exceeded. Try again after a short delay.";
                default:
                    return string.IsNullOrEmpty(exception.Message)
                        ? $"Google Sheets request failed ({(int)exception.HttpStatusCode})."
                        : exception.Message;
            }
        }
    }
}
