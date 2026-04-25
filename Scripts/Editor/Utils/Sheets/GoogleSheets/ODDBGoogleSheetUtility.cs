using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeamODD.ODDB.Editors.UI.Progress;
using TeamODD.ODDB.Runtime.Settings;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using UnityEditor.Compilation;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    public class ODDBGoogleSheetUtility
    {
        /// <summary>
        /// Task-based fetch helper used by <c>Backends.GoogleSheetsBackend</c>.
        /// Returns sheets parsed from the configured API endpoint without touching
        /// the local database. Legacy <see cref="LoadFromGoogleSheet"/> is retained
        /// for backwards compatibility.
        /// </summary>
        public static async Task<List<SheetInfo>> LoadSheetsAsync(CancellationToken ct)
        {
            ThrowIfSettingsNotReady();

            var url = BuildApiUrl();
            using (var request = UnityWebRequest.Get(url))
            {
                await request.SendWebRequest();
                ct.ThrowIfCancellationRequested();

                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException(
                        $"Google Sheets fetch failed: {request.error}");

                var jsonResponse = request.downloadHandler.text;
                var googleSheets = await Task.Run(
                    () => JsonConvert.DeserializeObject<List<GoogleSheet>>(jsonResponse),
                    ct);

                var result = new List<SheetInfo>();
                if (googleSheets == null) return result;
                foreach (var sheet in googleSheets)
                    result.Add(sheet.ToSheetInfo());
                return result;
            }
        }

        /// <summary>
        /// Task-based push helper used by <c>Backends.GoogleSheetsBackend</c>.
        /// Serializes <paramref name="sheets"/> and posts them to the configured
        /// endpoint. Legacy <see cref="SaveToGoogleSheet"/> is retained.
        /// </summary>
        public static async Task SaveSheetsAsync(IReadOnlyList<SheetInfo> sheets, CancellationToken ct)
        {
            if (sheets == null) throw new ArgumentNullException(nameof(sheets));
            ThrowIfSettingsNotReady();

            var payload = new List<GoogleSheet>(sheets.Count);
            foreach (var sheet in sheets)
            {
                if (sheet == null) continue;
                var googleSheet = new GoogleSheet();
                googleSheet.FromSheetInfo(sheet);
                payload.Add(googleSheet);
            }

            var jsonData = await Task.Run(() => JsonConvert.SerializeObject(payload), ct);

            var url = BuildApiUrl();
            using (var request = new UnityWebRequest(url, "POST"))
            {
                var bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest();
                ct.ThrowIfCancellationRequested();

                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException(
                        $"Google Sheets push failed: {request.error}");
            }
        }

        private static void ThrowIfSettingsNotReady()
        {
            if (ODDBSettings.Setting == null || !ODDBSettings.Setting.IsInitialized)
                throw new InvalidOperationException("ODDBSettings is not initialized.");
            if (string.IsNullOrEmpty(ODDBSettings.Setting.GoogleSheetAPIURL))
                throw new InvalidOperationException(
                    "GoogleSheetAPIURL is not configured in ODDBSettings.");
        }

        private static string BuildApiUrl()
        {
            return $"{ODDBSettings.Setting.GoogleSheetAPIURL}?secretKey={ODDBSettings.Setting.GoogleSheetAPISecretKey}";
        }

        [MenuItem(GoogleSheetConfig.MENU_ROOT + "Import from Google Sheets")]
        public static async void LoadFromGoogleSheet()
        {
            if (ODDBSettings.Setting.IsInitialized == false)
            {
                Debug.LogError("ODDB Settings is not initialized. Please set up ODDB before loading from Google Sheets.");
                return;
            }
            
            if (string.IsNullOrEmpty(ODDBSettings.Setting.GoogleSheetAPIURL))
            {
                Debug.LogError("Google Sheets API URL is not set in ODDB Settings.");
                return;
            }
            
            var url = $"{ODDBSettings.Setting.GoogleSheetAPIURL}?secretKey={ODDBSettings.Setting.GoogleSheetAPISecretKey}";
            
            try
            {
                using (var progress = ODDBProgressScope.Show("ODDB", "Loading data from Google Sheets...", 0.3f))
                {
                    using (var request = UnityWebRequest.Get(url))
                    {
                        await request.SendWebRequest();

                        if (request.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogError($"Error fetching data from Google Sheets: {request.error}");
                            return;
                        }

                        progress.Report("Parsing sheet data (Background)...", 0.6f);

                        var jsonResponse = request.downloadHandler.text;

                        // Parse JSON in background thread to avoid UI freeze
                        var sheetDataList = await Task.Run(() =>
                            JsonConvert.DeserializeObject<List<GoogleSheet>>(jsonResponse)
                        );

                        if (sheetDataList == null || sheetDataList.Count == 0)
                        {
                            Debug.LogWarning("No sheet data found in the response.");
                            return;
                        }

                        progress.Report("Processing sheets...", 0.9f);

                        for (var i = 0; i < sheetDataList.Count; i++)
                        {
                            var sheetData = sheetDataList[i];
                            var sheetName = string.IsNullOrEmpty(sheetData.Name) ? sheetData.ID : sheetData.Name;
                            progress.Report($"Processing downloaded sheet ({i + 1}/{sheetDataList.Count}): {sheetName}", 0.9f + 0.08f * ((i + 1f) / sheetDataList.Count));
                            Debug.Log($"Sheet Name: {sheetData.Name}, Sheet ID: {sheetData.ID}, Rows: {sheetData.Values.Count}");
                        }

                        var sheetConverter = new ODDBSheetConverter();
                        sheetConverter.SaveAllSheets(sheetDataList.Select(gs => gs.ToSheetInfo()).ToList());

                        Debug.Log($"Successfully loaded {sheetDataList.Count} sheets from Google Sheets.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing data from Google Sheets");
                Debug.LogException(e);
            }
            finally
            {
                CompilationPipeline.RequestScriptCompilation();
            }
        }

        [MenuItem(GoogleSheetConfig.MENU_ROOT + "Export to Google Sheets")]
        public static async void SaveToGoogleSheet()
        {
            if (ODDBSettings.Setting.IsInitialized == false)
            {
                Debug.LogError("ODDB Settings is not initialized. Please set up ODDB before saving to Google Sheets.");
                return;
            }
            
            if (ODDBSettings.Setting.DisableGoogleSheetExport)
            {
                Debug.LogError("Google Sheets export is disabled in ODDB Settings.");
                return;
            }
            
            if (string.IsNullOrEmpty(ODDBSettings.Setting.GoogleSheetAPIURL))
            {
                Debug.LogError("Google Sheets API URL is not set in ODDB Settings.");
                return;
            }
            
            var url = $"{ODDBSettings.Setting.GoogleSheetAPIURL}?secretKey={ODDBSettings.Setting.GoogleSheetAPISecretKey}";
            
            try
            {
                using (var progress = ODDBProgressScope.Show("ODDB", "Preparing sheets for export...", 0.2f))
                {
                    var sheetConverter = new ODDBSheetConverter();
                    var sheetInfoList = sheetConverter.GetAllSheets()
                        .Select(sheetInfo =>
                        {
                            var googleSheet = new GoogleSheet();
                            googleSheet.FromSheetInfo(sheetInfo);
                            return googleSheet;
                        })
                        .ToList();

                    if (sheetInfoList.Count == 0)
                    {
                        Debug.LogWarning("There are no sheets available to save.");
                        return;
                    }

                    Debug.Log($"Total Sheets to Save: {sheetInfoList.Count}");

                    for (var i = 0; i < sheetInfoList.Count; i++)
                    {
                        var sheetName = string.IsNullOrEmpty(sheetInfoList[i].Name) ? sheetInfoList[i].ID : sheetInfoList[i].Name;
                        progress.Report($"Preparing sheet for upload ({i + 1}/{sheetInfoList.Count}): {sheetName}", 0.2f + 0.15f * ((i + 1f) / sheetInfoList.Count));
                    }

                    progress.Report("Serializing data (Background)...", 0.4f);

                    // Serialize in background
                    var jsonData = await Task.Run(() => JsonConvert.SerializeObject(sheetInfoList));

                    progress.Report("Uploading to Google Sheets...", 0.6f);

                    using (var request = new UnityWebRequest(url, "POST"))
                    {
                        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                        request.downloadHandler = new DownloadHandlerBuffer();
                        request.SetRequestHeader("Content-Type", "application/json");

                        await request.SendWebRequest();

                        if (request.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogError($"Error saving data to Google Sheets: {request.error}");
                            return;
                        }

                        Debug.Log("Data successfully saved to Google Sheets.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving data to Google Sheets: {e.Message}");
            }
        }

        [MenuItem(GoogleSheetConfig.MENU_ROOT + "Create App Script")]
        public static void CreateGoogleSheetsAppScript()
        {
            if (ODDBSettings.Setting.IsInitialized == false)
            {
                Debug.LogError("ODDB Settings is not initialized. Please set up ODDB before creating Google Sheets App Script.");
                return;
            }
            
            var scriptPreset = Resources.Load<TextAsset>("GoogleSheetAppScript");
            if (scriptPreset == null)
            {
                Debug.LogError("Failed to load Google Sheets App Script preset from Resources.");
                return;
            }
            // replace
            var scriptContent = scriptPreset.text.Replace(GoogleSheetConfig.SECRET_REPLACE_TAG, ODDBSettings.Setting.GoogleSheetAPISecretKey);
            // save to clipboard
            EditorGUIUtility.systemCopyBuffer = scriptContent;
            var sb = new StringBuilder();
            sb.AppendLine("Google Sheets App Script has been created and copied to clipboard.");
            sb.AppendLine("Please paste it into your Google Sheets Script Editor!!");
            EditorUtility.DisplayDialog("ODDB", sb.ToString(), "OK");
        }
    }
}
