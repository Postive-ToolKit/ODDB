# Google Sheets API Integration Guide

ODDB imports and exports Google Sheets through Google Sheets API v4 with a service account. Apps Script deployment is no longer required.

## 1. Configure Google Cloud

1. Create or select a project in [Google Cloud Console](https://console.cloud.google.com/).
2. Enable `Google Sheets API` under `APIs & Services > Library`.
3. Create a service account under `IAM & Admin > Service Accounts`.
4. Create a JSON key for the service account and save it in a secure local folder.

The credential JSON contains a private key. Never place it under `Assets` or commit it to source control.

## 2. Share the Spreadsheet

1. Open the target Google Spreadsheet.
2. Click `Share`.
3. Add the `client_email` from the service-account JSON as an Editor.
4. Copy the Spreadsheet ID found between `/d/` and `/edit` in its URL.

## 3. Configure ODDB

1. Set `Google Spreadsheet Id` under `Google Sheets Settings` in `ODDBEditorSettings`.
2. Open `ODDB > Google Sheets > Setup` in Unity.
3. Select the service-account JSON with `Browse`.
4. Use `Test Connection` to verify authentication and sharing permissions.

The credential path is stored per user in `EditorPrefs`; it is not written to project assets.

## 4. Synchronize

- Select `Google Sheets` from the ODDB Editor Import or Export menu.
- The first export detects legacy Apps Script tabs and attaches ODDB metadata.
- Removing an ODDB-managed field physically deletes its Google Sheet column on the next export.
- ODDB asks for confirmation before deletion. User-managed columns and `#` comment rows are preserved.
- Removed rows continue to be marked with `#REMOVED`.

Field names currently identify managed columns. Renaming a field is treated as deleting the old column and adding a new one, so custom formatting on that column might not be preserved.
