using System;
using System.Threading;
using System.Threading.Tasks;
using Google;
using TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Settings
{
    [CustomEditor(typeof(ODDBEditorSettings))]
    internal sealed class ODDBEditorSettingsEditor : UnityEditor.Editor
    {
        private static readonly TimeSpan SignInTimeout = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan SignOutTimeout = TimeSpan.FromSeconds(10);

        private string _status;
        private MessageType _statusType = MessageType.Info;
        private bool _isBusy;
        private bool _cancelRequested;
        private string _activeOperation;
        private string _oauthClientId = string.Empty;
        private string _oauthClientSecret = string.Empty;
        private CancellationTokenSource _operationCancellation;

        private void OnEnable()
        {
            if (target is ODDBEditorSettings settings
                && settings.TryGetGoogleOAuthClientConfiguration(
                    out var clientId,
                    out var clientSecret,
                    out _))
            {
                _oauthClientId = clientId;
                _oauthClientSecret = clientSecret;
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);
            DrawGoogleOAuthSettings();
        }

        private void DrawGoogleOAuthSettings()
        {
            EditorGUILayout.LabelField("Google Sheets Authentication", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enter a Google OAuth Desktop Client ID and Client Secret. Credentials are encrypted in " +
                "ODDBEditorSettings; login tokens remain local to this operating-system user. " +
                "Sign in with a Google account that can edit the configured spreadsheet.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(_isBusy))
            {
                _oauthClientId = EditorGUILayout.TextField("OAuth Client ID", _oauthClientId);
                _oauthClientSecret = EditorGUILayout.PasswordField("OAuth Client Secret", _oauthClientSecret);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save OAuth Client", GUILayout.Height(22f)))
                        SaveOAuthClient();

                    using (new EditorGUI.DisabledScope(
                               !((ODDBEditorSettings)target).HasGoogleOAuthClientConfiguration))
                    {
                        if (GUILayout.Button("Clear", GUILayout.Height(22f), GUILayout.Width(90f)))
                            ClearOAuthClient();
                    }
                }
            }

            EditorGUILayout.LabelField(
                "Authorization",
                GoogleSheetsUserSettings.HasStoredAuthorization ? "Authorization saved" : "Not signed in");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           _isBusy || !((ODDBEditorSettings)target).HasGoogleOAuthClientConfiguration))
                {
                    var signInLabel = _activeOperation == "Google sign-in"
                        ? "Waiting for Google..."
                        : "Sign in with Google";
                    if (GUILayout.Button(signInLabel, GUILayout.Height(26f)))
                        _ = SignInAsync();
                }

                using (new EditorGUI.DisabledScope(
                           _isBusy || !GoogleSheetsUserSettings.HasStoredAuthorization))
                {
                    if (GUILayout.Button("Sign out", GUILayout.Height(26f), GUILayout.Width(90f)))
                        _ = SignOutAsync();
                }
            }

            using (new EditorGUI.DisabledScope(
                       _isBusy
                       || !((ODDBEditorSettings)target).HasGoogleOAuthClientConfiguration
                       || !GoogleSheetsUserSettings.HasStoredAuthorization))
            {
                var testLabel = _activeOperation == "connection test" ? "Testing..." : "Test Connection";
                if (GUILayout.Button(testLabel, GUILayout.Height(28f)))
                    _ = TestConnectionAsync();
            }

            if (_isBusy && GUILayout.Button("Cancel", GUILayout.Height(22f)))
                CancelCurrentOperation();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusType);
        }

        private void SaveOAuthClient()
        {
            try
            {
                var settings = (ODDBEditorSettings)target;
                Undo.RecordObject(settings, "Save Google OAuth Client");
                var changed = settings.SetGoogleOAuthClientConfiguration(
                    _oauthClientId,
                    _oauthClientSecret);
                if (changed)
                {
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    GoogleSheetsUserSettings.ClearAuthorization();
                    SetStatus(
                        "OAuth client saved encrypted in ODDBEditorSettings. Sign in with Google again.",
                        MessageType.Info);
                }
                else
                {
                    SetStatus("OAuth client configuration is unchanged.", MessageType.Info);
                }
            }
            catch (Exception e)
            {
                SetStatus(e.Message, MessageType.Error);
            }
        }

        private void ClearOAuthClient()
        {
            var settings = (ODDBEditorSettings)target;
            Undo.RecordObject(settings, "Clear Google OAuth Client");
            settings.ClearGoogleOAuthClientConfiguration();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            GoogleSheetsUserSettings.ClearAuthorization();
            _oauthClientId = string.Empty;
            _oauthClientSecret = string.Empty;
            SetStatus("OAuth client configuration and local authorization were cleared.", MessageType.Info);
        }

        private async Task SignInAsync()
        {
            await RunAsync(async ct =>
            {
                await GoogleSheetsOAuthClient.SignInAsync(ct);
                SetStatus("Google authorization completed.", MessageType.Info);
            }, SignInTimeout, "Google sign-in");
        }

        private async Task SignOutAsync()
        {
            await RunAsync(async ct =>
            {
                ct.ThrowIfCancellationRequested();
                await GoogleSheetsOAuthClient.SignOutAsync();
                SetStatus("Local Google authorization was removed.", MessageType.Info);
            }, SignOutTimeout, "Google sign-out");
        }

        private async Task TestConnectionAsync()
        {
            await RunAsync(async ct =>
            {
                serializedObject.ApplyModifiedProperties();
                var spreadsheetId = ((ODDBEditorSettings)target).GoogleSpreadsheetId?.Trim();
                if (string.IsNullOrEmpty(spreadsheetId))
                    throw new InvalidOperationException("Set Google Spreadsheet ID above first.");

                using (var service = await GoogleSheetsServiceFactory.CreateAsync(ct))
                {
                    var request = service.Spreadsheets.Get(spreadsheetId);
                    request.Fields = "spreadsheetId,properties.title";
                    var spreadsheet = await request.ExecuteAsync(ct);
                    SetStatus($"Connected: {spreadsheet.Properties?.Title ?? spreadsheet.SpreadsheetId}", MessageType.Info);
                }
            }, ConnectionTimeout, "connection test");
        }

        private async Task RunAsync(
            Func<CancellationToken, Task> action,
            TimeSpan timeout,
            string operationName)
        {
            var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(timeout);

            _operationCancellation = cancellation;
            _isBusy = true;
            _cancelRequested = false;
            _activeOperation = operationName;
            SetStatus(
                $"{operationName} in progress. This operation will time out after {FormatTimeout(timeout)}.",
                MessageType.Info);
            Repaint();

            try
            {
                await action(cancellation.Token);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                SetStatus(
                    _cancelRequested
                        ? $"{operationName} was cancelled."
                        : $"{operationName} timed out after {FormatTimeout(timeout)}.",
                    _cancelRequested ? MessageType.Info : MessageType.Error);
            }
            catch (GoogleApiException e)
            {
                SetStatus(GoogleSheetsApiError.Describe(e), MessageType.Error);
            }
            catch (Exception e)
            {
                SetStatus(e.Message, MessageType.Error);
            }
            finally
            {
                if (ReferenceEquals(_operationCancellation, cancellation))
                    _operationCancellation = null;
                cancellation.Dispose();
                _isBusy = false;
                _cancelRequested = false;
                _activeOperation = null;
                Repaint();
            }
        }

        private void CancelCurrentOperation()
        {
            if (!_isBusy || _operationCancellation == null)
                return;

            _cancelRequested = true;
            SetStatus("Cancelling " + _activeOperation + "...", MessageType.Info);
            _operationCancellation.Cancel();
        }

        private void OnDisable()
        {
            _operationCancellation?.Cancel();
        }

        private static string FormatTimeout(TimeSpan timeout)
        {
            return timeout.TotalMinutes >= 1
                ? $"{timeout.TotalMinutes:0} minute(s)"
                : $"{timeout.TotalSeconds:0} seconds";
        }

        private void SetStatus(string value, MessageType type)
        {
            _status = value;
            _statusType = type;
            Repaint();
        }
    }
}
