using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.Settings;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    public class ODDBGoogleSheetUtility
    {
        [MenuItem(GoogleSheetConfig.MENU_ROOT + "Load From Google Sheet")]
        public static void LoadFromGoogleSheet()
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
            
            EditorUtility.DisplayProgressBar("ODDB", "Loading data from Google Sheets...", 0.3f);
            
            var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Error fetching data from Google Sheets: {request.error}");
                        return;
                    }

                    EditorUtility.DisplayProgressBar("ODDB", "Parsing sheet data...", 0.6f);
                    
                    // 응답 데이터 파싱
                    var jsonResponse = request.downloadHandler.text;
                    var sheetDataList = JsonConvert.DeserializeObject<List<GoogleSheet>>(jsonResponse);

                    if (sheetDataList == null || sheetDataList.Count == 0)
                    {
                        Debug.LogWarning("No sheet data found in the response.");
                        return;
                    }
                    
                    EditorUtility.DisplayProgressBar("ODDB", "Processing sheets...", 0.9f);
                    
                    // 시트 정보 순차 출력
                    foreach (var sheetData in sheetDataList)
                    {
                        Debug.Log($"Sheet Name: {sheetData.Name}, Sheet ID: {sheetData.ID}, Rows: {sheetData.Values.Count}");
                    }
                    var sheetConverter = new ODDBSheetConverter();
                    sheetConverter.SaveAllSheets(sheetDataList.Select(gs => gs.ToSheetInfo()).ToList());
                    
                    Debug.Log($"✅ Successfully loaded {sheetDataList.Count} sheets from Google Sheets.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing data from Google Sheets: {e.Message}");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                    // refresh project
                    AssetDatabase.Refresh();
                }
            };
        }

        [MenuItem(GoogleSheetConfig.MENU_ROOT + "Save To Google Sheet")]
        public static void SaveToGoogleSheet()
        {
            if (ODDBSettings.Setting.IsInitialized == false)
            {
                Debug.LogError("ODDB Settings is not initialized. Please set up ODDB before saving to Google Sheets.");
                return;
            }
            
            if (string.IsNullOrEmpty(ODDBSettings.Setting.GoogleSheetAPIURL))
            {
                Debug.LogError("Google Sheets API URL is not set in ODDB Settings.");
                return;
            }
            
            var url = $"{ODDBSettings.Setting.GoogleSheetAPIURL}?secretKey={ODDBSettings.Setting.GoogleSheetAPISecretKey}";
            
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
                EditorUtility.ClearProgressBar();
                Debug.LogWarning("There are no sheets available to save.");
                return;
            }
            
            Debug.Log($"📄 Total Sheets to Save: {sheetInfoList.Count}");
            
            EditorUtility.DisplayProgressBar("ODDB", "Uploading to Google Sheets...", 0.5f);
            
            // post로 데이터 전송
            var request = new UnityWebRequest(url, nameof(UnityWebRequest.Post));
            var jsonData = JsonConvert.SerializeObject(sheetInfoList);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Error saving data to Google Sheets: {request.error}");
                        return;
                    }
                    
                    Debug.Log("✅ Data successfully saved to Google Sheets.");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            };
        }

        [MenuItem(GoogleSheetConfig.MENU_ROOT + "Create App Script for Google Sheets")]
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
