// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Text;
// using Newtonsoft.Json;
// using TeamODD.ODDB.Editors.Utils;
// using TeamODD.ODDB.Runtime.Data;
// using TeamODD.ODDB.Runtime.Data.DTO;
// using TeamODD.ODDB.Runtime.Data.Enum;
// using UnityEngine;
// using UnityEngine.Networking;
// using TeamODD.ODDB.Runtime.Settings;
// using TeamODD.ODDB.Runtime.Utils;
// using UnityEditor;
// using Plugins.ODDB.Scripts.Editor.Utils.Sheets;
//
// namespace Plugins.ODDB.Scripts.Editor.Utils
// {
//     public class ODDBGoogleSheetUtility
//     {
//         private const string SheetsApiBaseUrl = "https://sheets.googleapis.com/v4/spreadsheets";
//         
//         [MenuItem("ODDB/Load From Google Sheet")]
//         public static void LoadFromGoogleSheet()
//         {
//             GetAllSheetNames(OnSheetNamesReceived);
//         }
//
//         private static void OnSheetNamesReceived(bool success, string[] sheetNames, string error)
//         {
//             if (!success)
//             {
//                 Debug.LogError($"시트 리스트 가져오기 실패: {error}");
//                 return;
//             }
//             
//             LogSheetNames(sheetNames);
//             
//             if (sheetNames.Length <= 0)
//                 return;
//             
//             StartCoroutine(GetDataBase(sheetNames));
//         }
//
//         private static void LogSheetNames(string[] sheetNames)
//         {
//             var sb = new StringBuilder();
//             sb.AppendLine("=== Sheet List ===");
//             foreach (var sheetName in sheetNames)
//                 sb.AppendLine(sheetName);
//             sb.AppendLine("==============");
//             sb.AppendLine($"Total Sheets: {sheetNames.Length}");
//             Debug.Log(sb.ToString());
//         }
//
//         private static IEnumerator GetDataBase(string[] sheetNames)
//         {
//             var sheets = new List<GoogleSheetsResponse>();
//             
//             foreach (var sheetName in sheetNames)
//             {
//                 yield return GetAllSheetDataCoroutine(sheetName, args =>
//                 {
//                     if (args.IsSuccess)
//                     {
//                         Debug.Log($"데이터 가져오기 성공: {args.Response.SheetName} - {args.Response.Values.Length} rows");
//                         sheets.Add(args.Response);
//                     }
//                     else
//                     {
//                         Debug.LogError($"데이터 가져오기 실패: {args.Message}");
//                     }
//                 });
//             }
//             
//             ProcessSheetsData(sheets);
//         }
//
//         private static void ProcessSheetsData(List<GoogleSheetsResponse> sheets)
//         {
//             var viewList = new List<ODDBViewDTO>();
//             var tableList = new List<ODDBTableDTO>();
//             
//             foreach (var sheet in sheets)
//             {
//                 Debug.Log($"Processing sheet: {sheet.SheetName}");
//                 
//                 if (ODDBSheetConst.VIEW_SHEET_NAME.Equals(sheet.SheetName))
//                 {
//                     // Views 시트 처리
//                     var viewDtos = ConvertSheetToViewDtos(sheet);
//                     viewList.AddRange(viewDtos);
//                     Debug.Log($"View DTOs 추가: {viewDtos.Count}개");
//                 }
//                 else
//                 {
//                     // Table 시트로 처리
//                     var tableDto = ConvertToTableDto(sheet);
//                     if (tableDto != null)
//                     {
//                         tableList.Add(tableDto);
//                         Debug.Log($"Table DTO 추가: {sheet.SheetName}");
//                     }
//                 }
//             }
//             
//             var oddb = new ODDatabaseDTO(tableList, viewList);
//             SaveOddbData(oddb);
//         }
//
//         /// <summary>
//         /// Google Sheets Views 데이터를 ODDBViewDTO 리스트로 변환
//         /// </summary>
//         private static List<ODDBViewDTO> ConvertSheetToViewDtos(GoogleSheetsResponse response)
//         {
//             var viewList = new List<ODDBViewDTO>();
//             int currentRow = 0;
//             
//             while (currentRow < response.Values.Length)
//             {
//                 var viewData = ParseViewFromRow(response.Values, ref currentRow);
//                 if (viewData != null)
//                 {
//                     viewList.Add(viewData);
//                 }
//             }
//             
//             return viewList;
//         }
//
//         private static void SaveOddbData(ODDatabaseDTO oddb)
//         {
//             var dataService = new ODDBDataService();
//             var fullPath = Path.Combine(ODDBSettings.Setting.Path, ODDBSettings.Setting.DBName);
//             dataService.SaveFile(fullPath, JsonConvert.SerializeObject(oddb, Formatting.Indented));
//         }
//
//         private static string ConvertToViewRepoDto(GoogleSheetsResponse response)
//         {
//             var viewList = new List<string>();
//             int currentRow = 0;
//             
//             while (currentRow < response.Values.Length)
//             {
//                 var viewData = ParseViewFromRow(response.Values, ref currentRow);
//                 if (viewData != null)
//                 {
//                     viewList.Add(JsonConvert.SerializeObject(viewData));
//                 }
//             }
//             
//             return JsonConvert.SerializeObject(viewList, Formatting.Indented);
//         }
//
//         private static ODDBViewDTO ParseViewFromRow(string[][] values, ref int currentRow)
//         {
//             // 빈 행이거나 View로 시작하지 않는 행 건너뛰기
//             while (currentRow < values.Length)
//             {
//                 var row = values[currentRow];
//                 if (row.Length > 0 && !string.IsNullOrEmpty(row[0]) && 
//                     row[0].Equals("View", StringComparison.OrdinalIgnoreCase))
//                 {
//                     break;
//                 }
//                 currentRow++;
//             }
//
//             if (currentRow >= values.Length)
//                 return null;
//
//             var viewRow = values[currentRow];
//             if (viewRow.Length < 5)
//             {
//                 Debug.LogError($"Invalid view definition at row {currentRow + 1}. Expected at least 5 columns.");
//                 currentRow++;
//                 return null;
//             }
//
//             var viewKey = viewRow[1];
//             var viewName = viewRow[2];
//             var bindType = viewRow[3];
//             var parentView = viewRow[4];
//             
//             currentRow += 2; // View 행과 Table Meta 행 건너뛰기
//             var tableMetas = ParseTableMetas(values, ref currentRow);
//
//             Debug.Log($"Found view: {viewName} (Key: {viewKey}, BindType: {bindType}, ParentView: {parentView})");
//             return new ODDBViewDTO(viewName, viewKey, tableMetas, bindType, parentView);
//         }
//
//         private static List<ODDBField> ParseTableMetas(string[][] values, ref int currentRow)
//         {
//             var tableMetas = new List<ODDBField>();
//             
//             while (currentRow < values.Length)
//             {
//                 var metaRow = values[currentRow];
//                 if (metaRow.Length == 0)
//                 {
//                     currentRow++;
//                     break; // 빈행이면 종료
//                 }
//                 
//                 if (metaRow.Length < 4)
//                 {
//                     Debug.LogError($"Invalid table meta definition at row {currentRow + 1}. Expected at least 3 columns.");
//                     currentRow++;
//                     continue;
//                 }
//
//                 var fieldId = metaRow[1];
//                 var fieldName = metaRow[2];
//                 var fieldTypeStr = metaRow[3];
//                 
//                 if (!Enum.TryParse(fieldTypeStr, true, out ODDBDataType fieldType))
//                 {
//                     Debug.LogWarning($"Unknown data type '{fieldTypeStr}' at row {currentRow + 1}. Defaulting to String.");
//                     fieldType = ODDBDataType.String;
//                 }
//                 
//                 tableMetas.Add(new ODDBField(new ODDBID(fieldId), fieldName, fieldType));
//                 currentRow++;
//             }
//             
//             return tableMetas;
//         }
//
//         /// <summary>
//         /// Google Sheets 응답을 ODDBTableDTO로 변환
//         /// </summary>
//         private static ODDBTableDTO ConvertToTableDto(GoogleSheetsResponse response)
//         {
//             try
//             {
//                 if (response.Values == null || response.Values.Length < 4)
//                 {
//                     Debug.LogError($"Table 시트 데이터가 부족합니다: {response.SheetName}");
//                     return null;
//                 }
//
//                 var values = response.Values;
//                 
//                 // 첫 번째 행: Table 헤더 파싱
//                 var tableHeaderRow = values[0];
//                 if (tableHeaderRow.Length < 5 || !tableHeaderRow[0].Equals("Table", StringComparison.OrdinalIgnoreCase))
//                 {
//                     Debug.LogError($"잘못된 Table 헤더: {response.SheetName}");
//                     return null;
//                 }
//
//                 var tableId = tableHeaderRow[1];
//                 var tableName = tableHeaderRow[2];
//                 var bindType = tableHeaderRow[3];
//                 var parentView = tableHeaderRow[4];
//
//                 // 두 번째 행: View 매핑 (건너뜀, 시트에서는 View 정보를 별도 관리)
//                 
//                 // 세 번째 행: 컬럼 이름들
//                 var columnNamesRow = values[2];
//                 
//                 // 네 번째 행: 데이터 타입들
//                 var dataTypesRow = values[3];
//
//                 // TableMetas 생성
//                 var tableMetas = new List<ODDBField>();
//                 var columnCount = Math.Min(columnNamesRow.Length, dataTypesRow.Length);
//                 
//                 for (int i = 0; i < columnCount; i++)
//                 {
//                     var columnName = columnNamesRow[i];
//                     var dataTypeStr = dataTypesRow[i];
//                     
//                     if (!Enum.TryParse(dataTypeStr, true, out ODDBDataType dataType))
//                     {
//                         Debug.LogWarning($"Unknown data type '{dataTypeStr}' in column {i}. Defaulting to String.");
//                         dataType = ODDBDataType.String;
//                     }
//                     
//                     // 컬럼별로 고유 ID 생성 (임시)
//                     var fieldId = $"{tableId}_{i}";
//                     tableMetas.Add(new ODDBField(new ODDBID(fieldId), columnName, dataType));
//                 }
//
//                 // 실제 데이터 행들 처리 (5번째 행부터)
//                 var dataRows = new List<string[]>();
//                 for (int rowIndex = 4; rowIndex < values.Length; rowIndex++)
//                 {
//                     var dataRow = values[rowIndex];
//                     var paddedRow = new string[columnCount];
//                     
//                     // 데이터 복사 (부족한 컬럼은 빈 문자열로 채움)
//                     for (int colIndex = 0; colIndex < columnCount; colIndex++)
//                     {
//                         paddedRow[colIndex] = colIndex < dataRow.Length ? dataRow[colIndex] ?? "" : "";
//                     }
//                     
//                     dataRows.Add(paddedRow);
//                 }
//
//                 var tableDto = new ODDBTableDTO(
//                     tableName,
//                     tableId,
//                     tableMetas,
//                     bindType,
//                     parentView,
//                     dataRows.ToArray()
//                 );
//
//                 Debug.Log($"✅ Google Sheets → Table DTO 변환: {tableName} " +
//                          $"({columnCount}개 컬럼, {dataRows.Count}개 데이터 행)");
//                 
//                 return tableDto;
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Table DTO 변환 실패: {response.SheetName} - {e.Message}");
//                 return null;
//             }
//         }
//
//         #region Sheet Names API
//         
//         public static void GetAllSheetNames(Action<bool, string[], string> onComplete = null)
//         {
//             if (!ValidateSettings(out string error))
//             {
//                 onComplete?.Invoke(false, Array.Empty<string>(), error);
//                 return;
//             }
//             
//             StartCoroutine(GetAllSheetNamesCoroutine(onComplete));
//         }
//         
//         private static IEnumerator GetAllSheetNamesCoroutine(Action<bool, string[], string> onComplete)
//         {
//             string url = $"{SheetsApiBaseUrl}/{ODDBSettings.Setting.GoogleSheetsID}" +
//                         $"?key={ODDBSettings.Setting.GoogleSheetApiKey}&fields=sheets.properties.title";
//             
//             using var request = UnityWebRequest.Get(url);
//             request.SetRequestHeader("Accept", "application/json");
//             
//             yield return request.SendWebRequest();
//             
//             if (request.result == UnityWebRequest.Result.Success)
//             {
//                 try
//                 {
//                     var responseText = request.downloadHandler.text;
//                     Debug.Log($"Spreadsheet metadata response: {responseText}");
//                     
//                     var sheetNames = ParseSheetNamesFromResponse(responseText);
//                     onComplete?.Invoke(true, sheetNames, "");
//                 }
//                 catch (Exception e)
//                 {
//                     onComplete?.Invoke(false, Array.Empty<string>(), $"Error parsing sheet names: {e.Message}");
//                 }
//             }
//             else
//             {
//                 var errorMessage = FormatWebRequestError(request);
//                 onComplete?.Invoke(false, Array.Empty<string>(), errorMessage);
//             }
//         }
//         
//         private static string[] ParseSheetNamesFromResponse(string jsonResponse)
//         {
//             try
//             {
//                 var response = JsonUtility.FromJson<SpreadsheetMetadataResponse>(jsonResponse);
//                 
//                 if (response?.sheets == null || response.sheets.Length == 0)
//                     return Array.Empty<string>();
//                 
//                 var sheetNames = new string[response.sheets.Length];
//                 for (int i = 0; i < response.sheets.Length; i++)
//                 {
//                     sheetNames[i] = response.sheets[i].properties.title;
//                 }
//                 
//                 return sheetNames;
//             }
//             catch (Exception e)
//             {
//                 throw new Exception($"Failed to parse sheet names from response: {e.Message}");
//             }
//         }
//         
//         #endregion
//
//         #region Sheet Data API
//         
//         public static void GetAllSheetData(string sheetName, Action<OnGoogleSheetDataRequest> onComplete = null)
//         {
//             if (!ValidateSettings(out string error))
//             {
//                 var failedRequest = OnGoogleSheetDataRequest.Failed;
//                 failedRequest.Message = error;
//                 onComplete?.Invoke(failedRequest);
//                 return;
//             }
//             
//             StartCoroutine(GetAllSheetDataCoroutine(sheetName, onComplete));
//         }
//         
//         private static IEnumerator GetAllSheetDataCoroutine(string sheetName, Action<OnGoogleSheetDataRequest> onComplete)
//         {
//             var url = $"{SheetsApiBaseUrl}/{ODDBSettings.Setting.GoogleSheetsID}/values/{sheetName}" +
//                      $"?key={ODDBSettings.Setting.GoogleSheetApiKey}";
//
//             using var request = UnityWebRequest.Get(url);
//             request.SetRequestHeader("Accept", "application/json");
//                 
//             yield return request.SendWebRequest();
//                 
//             if (request.result != UnityWebRequest.Result.Success)
//             {
//                 var errorMessage = FormatWebRequestError(request);
//                 var errorRequest = OnGoogleSheetDataRequest.Failed;
//                 errorRequest.Message = errorMessage;
//                 onComplete?.Invoke(errorRequest);
//                 yield break;
//             }
//             
//             try
//             {
//                 var responseText = request.downloadHandler.text;
//                 Debug.Log($"Raw API Response: {responseText}");
//                 
//                 var response = JsonConvert.DeserializeObject<GoogleSheetsResponse>(responseText);
//                 if (response == null)
//                     throw new Exception("Response deserialization resulted in null");
//                 
//                 response.SheetName = sheetName;
//                 var dataRequest = OnGoogleSheetDataRequest.Success;
//                 dataRequest.Response = response;
//                 onComplete?.Invoke(dataRequest);
//             }
//             catch (Exception e)
//             {
//                 var errorRequest = OnGoogleSheetDataRequest.Failed;
//                 errorRequest.Message = $"Error parsing response: {e.Message}";
//                 onComplete?.Invoke(errorRequest);
//             }
//         }
//         
//         #endregion
//
//         #region Export to Google Sheets
//         
//         [MenuItem("ODDB/Export Views to Google Sheet")]
//         public static void ExportViewsToGoogleSheet()
//         {
//             try
//             {
//                 // 새로운 ODDBSheetConverter 사용
//                 var sheetData = ODDBSheetConverter.ConvertODDBToSheetData();
//                 
//                 if (sheetData == null || sheetData.Count == 0)
//                 {
//                     Debug.LogWarning("업로드할 Views 데이터가 없습니다.");
//                     return;
//                 }
//
//                 UploadToGoogleSheet(ODDBSheetConst.VIEW_SHEET_NAME, sheetData);
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Views 업로드 실패: {e.Message}");
//             }
//         }
//
//         [MenuItem("ODDB/Debug/Test Sheet Conversion")]
//         public static void TestSheetConversion()
//         {
//             ODDBSheetConverter.DebugConversionProcess();
//         }
//
//         private static void UploadToGoogleSheet(string sheetName, List<List<string>> data)
//         {
//             if (!ValidateSettings(out string error))
//             {
//                 Debug.LogError($"설정 검증 실패: {error}");
//                 return;
//             }
//             
//             StartCoroutine(UploadToGoogleSheetCoroutine(sheetName, data));
//         }
//
//         private static IEnumerator UploadToGoogleSheetCoroutine(string sheetName, List<List<string>> data)
//         {
//             Debug.Log($"Google Sheets 업로드 시작: {sheetName}");
//             
//             // 먼저 시트를 클리어
//             yield return ClearSheetCoroutine(sheetName);
//             
//             // 데이터 업로드
//             var uploadData = new GoogleSheetsUploadRequest
//             {
//                 range = $"{sheetName}!A1",
//                 majorDimension = "ROWS",
//                 values = ConvertToStringArray(data)
//             };
//
//             var json = JsonConvert.SerializeObject(uploadData);
//             
//             // URL 구조 수정: range를 URL 경로에 포함하고 쿼리 파라미터 정리
//             var range = Uri.EscapeDataString($"{sheetName}!A1:Z1000");
//             var url = $"{SheetsApiBaseUrl}/{ODDBSettings.Setting.GoogleSheetsID}/values/{range}" +
//                      $"?valueInputOption=RAW&key={ODDBSettings.Setting.GoogleSheetApiKey}";
//
//             using var request = new UnityWebRequest(url, "PUT");
//             var bodyRaw = Encoding.UTF8.GetBytes(json);
//             request.uploadHandler = new UploadHandlerRaw(bodyRaw);
//             request.downloadHandler = new DownloadHandlerBuffer();
//             request.SetRequestHeader("Content-Type", "application/json");
//             request.SetRequestHeader("Accept", "application/json");
//
//             yield return request.SendWebRequest();
//
//             if (request.result == UnityWebRequest.Result.Success)
//             {
//                 Debug.Log($"✅ Views 업로드 성공: {sheetName}");
//                 Debug.Log($"응답: {request.downloadHandler.text}");
//             }
//             else
//             {
//                 var errorMessage = FormatWebRequestError(request);
//                 Debug.LogError($"❌ Views 업로드 실패: {errorMessage}");
//                 Debug.LogError($"응답 내용: {request.downloadHandler.text}");
//                 
//                 // 401 에러인 경우 특별한 안내 메시지
//                 if (request.responseCode == 401)
//                 {
//                     Debug.LogWarning("💡 해결 방법:\n" +
//                                    "1. Google Sheets를 '링크가 있는 모든 사용자가 편집 가능'으로 공유했는지 확인\n" +
//                                    "2. Google Cloud Console에서 Google Sheets API가 활성화되어 있는지 확인\n" +
//                                    "3. API 키가 올바른지 확인");
//                 }
//             }
//         }
//
//         private static IEnumerator ClearSheetCoroutine(string sheetName)
//         {
//             Debug.Log($"시트 클리어 시작: {sheetName}");
//             
//             // Clear API 엔드포인트 수정
//             var range = Uri.EscapeDataString($"{sheetName}!A1:Z1000");
//             var url = $"{SheetsApiBaseUrl}/{ODDBSettings.Setting.GoogleSheetsID}/values/{range}:clear" +
//                      $"?key={ODDBSettings.Setting.GoogleSheetApiKey}";
//
//             using var request = new UnityWebRequest(url, "POST");
//             request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
//             request.downloadHandler = new DownloadHandlerBuffer();
//             request.SetRequestHeader("Content-Type", "application/json");
//
//             yield return request.SendWebRequest();
//
//             if (request.result == UnityWebRequest.Result.Success)
//             {
//                 Debug.Log($"✅ 시트 클리어 성공: {sheetName}");
//             }
//             else
//             {
//                 Debug.LogWarning($"⚠️ 시트 클리어 실패 (계속 진행): {FormatWebRequestError(request)}");
//                 // 클리어 실패해도 업로드는 계속 진행
//             }
//         }
//
//         private static string[][] ConvertToStringArray(List<List<string>> data)
//         {
//             var result = new string[data.Count][];
//             for (int i = 0; i < data.Count; i++)
//             {
//                 result[i] = data[i].ToArray();
//             }
//             return result;
//         }
//
//         #endregion
//
//         #region Helper Classes for Export
//
//         [Serializable]
//         private class GoogleSheetsUploadRequest
//         {
//             [JsonProperty("range")]
//             public string range;
//             
//             [JsonProperty("majorDimension")]
//             public string majorDimension;
//             
//             [JsonProperty("values")]
//             public string[][] values;
//         }
//
//         #endregion
//
//         #region Helper Classes
//
//         [Serializable]
//         private class SpreadsheetMetadataResponse
//         {
//             public SheetInfo[] sheets;
//         }
//
//         [Serializable]
//         private class SheetInfo
//         {
//             public SheetProperties properties;
//         }
//
//         [Serializable]
//         private class SheetProperties
//         {
//             public string title;
//         }
//
//         private class CoroutineRunner : MonoBehaviour
//         {
//             // MonoBehaviour는 코루틴 실행을 위해서만 사용
//         }
//         
//         #endregion
//
//         #region Utility Methods
//         
//         private static bool ValidateSettings(out string error)
//         {
//             var settings = ODDBSettings.Setting;
//             error = "";
//             
//             if (settings == null)
//             {
//                 error = "ODDBSettings not found";
//                 return false;
//             }
//             
//             if (string.IsNullOrEmpty(settings.GoogleSheetApiKey))
//             {
//                 error = "Google Sheets API key is not set in ODDBSettings";
//                 return false;
//             }
//             
//             if (string.IsNullOrEmpty(settings.GoogleSheetsID))
//             {
//                 error = "Google Sheets spreadsheet ID is not set in ODDBSettings";
//                 return false;
//             }
//             
//             return true;
//         }
//         
//         private static string FormatWebRequestError(UnityWebRequest request)
//         {
//             var errorMessage = $"HTTP Error: {request.error}\nResponse Code: {request.responseCode}";
//             
//             return request.responseCode switch
//             {
//                 403 => errorMessage + "\nAPI key may be invalid or Google Sheets API is not enabled.",
//                 404 => errorMessage + "\nSpreadsheet not found or not accessible.",
//                 _ => errorMessage
//             };
//         }
//         
//         private static void StartCoroutine(IEnumerator coroutine)
//         {
//             var tempGo = new GameObject("TempGoogleSheetsUtility");
//             var coroutineRunner = tempGo.AddComponent<CoroutineRunner>();
//             coroutineRunner.StartCoroutine(ExecuteAndCleanup(coroutine, tempGo));
//         }
//         
//         private static IEnumerator ExecuteAndCleanup(IEnumerator coroutine, GameObject tempGo)
//         {
//             yield return coroutine;
//             UnityEngine.Object.DestroyImmediate(tempGo);
//         }
//         
//         #endregion
//     }
// }
