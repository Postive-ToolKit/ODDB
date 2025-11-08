# Google Sheets Integration Guide
To integrate with Google Sheets, you need to use Google Sheets' Apps Script. Follow the guide below to complete the setup.

## 1. Creating Apps Script
ODDB provides a feature to automatically generate the script for integration. Follow these steps:
1. Set the `Google Sheet API Secret Key` in the `Google Sheets Settings` section of the ODDB Setting file.
2. Click `ODDB/Google Sheets/Create App Script` from the ODDB top menu.
3. Once the script is generated, it will be automatically copied to your clipboard.

## 2. Adding Apps Script to Google Sheets
<img width="899" height="357" alt="image" src="https://github.com/user-attachments/assets/b3a5f0c3-a550-4bec-976c-d177d0c98a81" />

1. Open your Google Sheet.
2. Click `Extensions > Apps Script` from the top menu.

<img width="1056" height="323" alt="image" src="https://github.com/user-attachments/assets/61a0c970-21a7-41db-8717-293a06eaa6c7" />

3. Delete all existing code in the newly opened Apps Script editor.
4. Paste the script copied to your clipboard.

## 3. Deploying Apps Script
<img width="1124" height="321" alt="image" src="https://github.com/user-attachments/assets/1e5bec3a-ea47-43c6-96fd-5df4696d2fc6" />

1. Click `Deploy > New deployment` from the top menu of the Apps Script editor.

<img width="755" height="373" alt="image" src="https://github.com/user-attachments/assets/def3dc85-32e8-4f92-a520-2701fb345598" />

2. Select `Web app` for the `Deployment type`.

<img width="761" height="598" alt="image" src="https://github.com/user-attachments/assets/d52e98e8-7413-4890-84a1-03d94d309fa9" />

3. Enter a description for the deployment in the `Description` field.
4. Set the `Who has access` option to `Anyone (including anonymous users)`.
5. Click the `Deploy` button.

## 3.1 Authorizing Permissions
1. During the deployment process, you may need to authorize permissions. Click the `Authorize access` button.
2. Select your Google account.
3. Click `Advanced` and then click the `Go to [project name] (unsafe)` link.
4. Click the `Allow` button to grant the permissions.

## 4. Copying the Web App URL
<img width="758" height="589" alt="image" src="https://github.com/user-attachments/assets/cb17bb6d-de1d-4770-ae4c-b89aac0a824c" />

1. Copy the `Web app URL` displayed after deployment is complete.
2. Paste the copied URL into the `Google Sheets API URL` field in the `Google Sheets Settings` section of the ODDB Setting file.

---
Integration between ODDB and Google Sheets is now complete. You can use the `ODDB/Google Sheets/Import from Google Sheets` and `ODDB/Google Sheets/Export to Google Sheets` options from the ODDB top menu to import and export data.

