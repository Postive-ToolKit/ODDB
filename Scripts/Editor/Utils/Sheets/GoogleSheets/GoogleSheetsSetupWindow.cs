using System;
using System.Threading.Tasks;
using Google;
using TeamODD.ODDB.Editors.Settings;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal sealed class GoogleSheetsSetupWindow : EditorWindow
    {
        private string _credentialPath;
        private string _serviceAccountEmail;
        private string _status;
        private bool _isTesting;

        [MenuItem(GoogleSheetConfig.MENU_ROOT + "Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<GoogleSheetsSetupWindow>(true, "ODDB Google Sheets Setup");
            window.minSize = new Vector2(560f, 250f);
            window.Show();
        }

        private void OnEnable()
        {
            _credentialPath = GoogleSheetsUserSettings.CredentialPath;
            _ = RefreshEmailAsync();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Google Sheets API v4", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enable Google Sheets API in a Google Cloud project, create a service account, " +
                "and share the target spreadsheet with the service-account email as an Editor. " +
                "The credential path is stored per user in EditorPrefs and is not written to Assets.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Credential JSON");
                EditorGUILayout.SelectableLabel(
                    string.IsNullOrEmpty(_credentialPath) ? "Not selected" : _credentialPath,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                    SelectCredential();
                if (GUILayout.Button("Clear", GUILayout.Width(60f)))
                    ClearCredential();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Service Account");
                EditorGUILayout.SelectableLabel(
                    string.IsNullOrEmpty(_serviceAccountEmail) ? "-" : _serviceAccountEmail,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_serviceAccountEmail)))
                {
                    if (GUILayout.Button("Copy", GUILayout.Width(80f)))
                        EditorGUIUtility.systemCopyBuffer = _serviceAccountEmail;
                }
            }

            var spreadsheetId = ODDBEditorSettings.Setting.GoogleSpreadsheetId;
            EditorGUILayout.LabelField("Spreadsheet ID", string.IsNullOrEmpty(spreadsheetId) ? "Not configured" : spreadsheetId);

            using (new EditorGUI.DisabledScope(_isTesting))
            {
                if (GUILayout.Button(_isTesting ? "Testing..." : "Test Connection", GUILayout.Height(28f)))
                    _ = TestConnectionAsync();
            }

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _status.StartsWith("Connected", StringComparison.Ordinal) ? MessageType.Info : MessageType.Error);
        }

        private void SelectCredential()
        {
            var selected = EditorUtility.OpenFilePanel("Select Google service-account JSON", string.Empty, "json");
            if (string.IsNullOrEmpty(selected))
                return;

            GoogleSheetsUserSettings.CredentialPath = selected;
            _credentialPath = GoogleSheetsUserSettings.CredentialPath;
            _status = string.Empty;
            _ = RefreshEmailAsync();
        }

        private void ClearCredential()
        {
            GoogleSheetsUserSettings.CredentialPath = string.Empty;
            _credentialPath = string.Empty;
            _serviceAccountEmail = string.Empty;
            _status = string.Empty;
        }

        private async Task RefreshEmailAsync()
        {
            try
            {
                _serviceAccountEmail = await GoogleSheetsServiceFactory.ReadServiceAccountEmailAsync();
            }
            catch
            {
                _serviceAccountEmail = string.Empty;
            }
            Repaint();
        }

        private async Task TestConnectionAsync()
        {
            _isTesting = true;
            _status = string.Empty;
            Repaint();

            try
            {
                var spreadsheetId = ODDBEditorSettings.Setting.GoogleSpreadsheetId;
                if (string.IsNullOrWhiteSpace(spreadsheetId))
                    throw new InvalidOperationException("Set Google Spreadsheet ID in ODDBEditorSettings first.");

                using (var service = GoogleSheetsServiceFactory.Create())
                {
                    var request = service.Spreadsheets.Get(spreadsheetId.Trim());
                    request.Fields = "spreadsheetId,properties.title";
                    var spreadsheet = await request.ExecuteAsync();
                    _status = $"Connected: {spreadsheet.Properties?.Title ?? spreadsheet.SpreadsheetId}";
                }
            }
            catch (GoogleApiException e)
            {
                _status = GoogleSheetsApiError.Describe(e);
            }
            catch (Exception e)
            {
                _status = e.Message;
            }
            finally
            {
                _isTesting = false;
                Repaint();
            }
        }
    }
}
