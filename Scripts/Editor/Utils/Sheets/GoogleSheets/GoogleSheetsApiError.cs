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
                    return "Google Sheets access was denied. Enable Google Sheets API and sign in with an account that can edit the spreadsheet.";
                case HttpStatusCode.NotFound:
                    return "The configured Google spreadsheet was not found. Check the Spreadsheet ID and the signed-in account's access.";
                case HttpStatusCode.Unauthorized:
                    return "Google OAuth authentication failed. Sign out, then sign in with Google again.";
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
