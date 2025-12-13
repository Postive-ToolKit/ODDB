using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.Settings;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using UnityEditor.Compilation;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    public class ODDBGoogleSheetUtility
    {
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
                EditorUtility.DisplayProgressBar("ODDB", "Loading data from Google Sheets...", 0.3f);
                
                using (var request = UnityWebRequest.Get(url))
                {
                    await request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Error fetching data from Google Sheets: {request.error}");
                        return;
                    }

                    EditorUtility.DisplayProgressBar("ODDB", "Parsing sheet data (Background)...", 0.6f);
                    
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
                    
                    EditorUtility.DisplayProgressBar("ODDB", "Processing sheets...", 0.9f);
                    
                    foreach (var sheetData in sheetDataList)
                    {
                        Debug.Log($"Sheet Name: {sheetData.Name}, Sheet ID: {sheetData.ID}, Rows: {sheetData.Values.Count}");
                    }
                    
                    var sheetConverter = new ODDBSheetConverter();
                    sheetConverter.SaveAllSheets(sheetDataList.Select(gs => gs.ToSheetInfo()).ToList());
                    
                    Debug.Log($"✅ Successfully loaded {sheetDataList.Count} sheets from Google Sheets.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing data from Google Sheets");
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
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
                EditorUtility.DisplayProgressBar("ODDB", "Preparing sheets for export...", 0.2f);
                
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
                
                Debug.Log($"📄 Total Sheets to Save: {sheetInfoList.Count}");
                
                EditorUtility.DisplayProgressBar("ODDB", "Serializing data (Background)...", 0.4f);

                // Serialize in background
                var jsonData = await Task.Run(() => JsonConvert.SerializeObject(sheetInfoList));
                
                EditorUtility.DisplayProgressBar("ODDB", "Uploading to Google Sheets...", 0.6f);
                
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
                    
                    Debug.Log("✅ Data successfully saved to Google Sheets.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving data to Google Sheets: {e.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
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