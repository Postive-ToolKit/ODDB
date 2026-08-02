# Google Sheets API Integration Guide

ODDB imports and exports Google Sheets through Google Sheets API v4 with Google OAuth user authorization. Apps Script deployment and service-account spreadsheet sharing are not required.

## 1. Create a Google OAuth Client

1. Create or select a project in [Google Cloud Console](https://console.cloud.google.com/).
2. Enable `Google Sheets API` under `APIs & Services > Library`.
3. Configure the OAuth consent screen and its audience.
4. Create an OAuth 2.0 Client with application type `Desktop app`.
5. Copy the generated Client ID and Client Secret.

## 2. Prepare the Spreadsheet

1. Open the target Google Spreadsheet.
2. Click `Share`.
3. Confirm that the Google account you will authorize in Unity has Editor access.
4. Copy the Spreadsheet ID found between `/d/` and `/edit` in its URL.

## 3. Configure ODDB

1. Select the `ODDBEditorSettings` asset.
2. Enter the Desktop OAuth Client ID and Client Secret under `Google Sheets Authentication`, then click `Save OAuth Client`.
3. Set `Google Spreadsheet Id` under `Google Sheets Settings`.
4. Click `Sign in with Google` and complete authorization in the browser.
5. Use `Test Connection` to verify authentication and spreadsheet access.

If Import or Export is selected before configuration is complete, ODDB displays an alert and selects `ODDBEditorSettings` in the Inspector.

OAuth client credentials are encrypted before they are serialized into `ODDBEditorSettings`. This prevents plaintext exposure but is not a substitute for repository access control, because ODDB must also contain the decryption logic. Keep projects containing the credentials private. OAuth tokens are stored under the user's local application-data folder and are not written to project assets.

## 4. Synchronize

- Select `Google Sheets` from the ODDB Editor Import or Export menu.
- The first export detects legacy Apps Script tabs and attaches ODDB metadata.
- Removing an ODDB-managed field physically deletes its Google Sheet column on the next export.
- ODDB asks for confirmation before deletion. User-managed columns and `#` comment rows are preserved.
- Removed rows continue to be marked with `#REMOVED`.

Field names currently identify managed columns. Renaming a field is treated as deleting the old column and adding a new one, so custom formatting on that column might not be preserved.
