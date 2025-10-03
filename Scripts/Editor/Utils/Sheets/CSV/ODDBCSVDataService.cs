// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Text;
// using System.Linq;
// using UnityEngine;
// using UnityEditor;
// using Plugins.ODDB.Scripts.Editor.Utils.Sheets;
// using TeamODD.ODDB.Runtime.Data.DTO;
// using TeamODD.ODDB.Runtime.Data;
// using TeamODD.ODDB.Editors.Utils;
// using TeamODD.ODDB.Runtime.Settings;
//
// namespace Plugins.ODDB.Scripts.Editor.Utils.Sheets.CSV
// {
//     public class ODDBCSVDataService
//     {
//         [MenuItem("ODDB/Export All Sheets to CSV")]
//         public static void ExportAllSheetsToCSV()
//         {
//             try
//             {
//                 var sheetConverter = new ODDBSheetConverter();
//                 // ODDBSheetConverter를 사용하여 모든 SheetInfo 리스트 생성
//                 var sheetInfoList = sheetConverter.ConvertODDBToSheetInfoList();
//
//                 if (sheetInfoList == null || sheetInfoList.Count == 0)
//                 {
//                     Debug.LogWarning("내보낼 시트 데이터가 없습니다.");
//                     return;
//                 }
//
//                 Debug.Log($"📊 총 {sheetInfoList.Count}개 시트를 CSV로 내보냅니다.");
//
//                 // 저장 폴더 선택
//                 var baseDirectory = EditorUtility.OpenFolderPanel("CSV 파일들을 저장할 폴더 선택", "", "");
//                 if (string.IsNullOrEmpty(baseDirectory))
//                 {
//                     Debug.Log("CSV 저장이 취소되었습니다.");
//                     return;
//                 }
//
//                 // 각 시트를 개별 CSV 파일로 저장
//                 var savedCount = 0;
//
//                 foreach (var sheetInfo in sheetInfoList)
//                 {
//                     if (sheetInfo.IsEmpty)
//                         continue;
//
//                     var csvContent = ConvertSheetInfoToCSV(sheetInfo);
//                     var fileName = $"{sheetInfo.SheetName}.csv";
//                     var filePath = Path.Combine(baseDirectory, fileName);
//
//                     try
//                     {
//                         var utf8WithBom = new UTF8Encoding(true);
//                         File.WriteAllText(filePath, csvContent, utf8WithBom);
//                         savedCount++;
//
//                         Debug.Log($"✅ CSV 저장 완료: {fileName} ({sheetInfo.RowCount}행)");
//                     }
//                     catch (Exception e)
//                     {
//                         Debug.LogError($"❌ CSV 저장 실패: {fileName} - {e.Message}");
//                     }
//                 }
//
//                 Debug.Log($"🎉 CSV 내보내기 완료: {savedCount}/{sheetInfoList.Count}개 파일 저장됨");
//
//                 // 저장된 폴더를 탐색기에서 열기
//                 OpenDirectoryInExplorer(baseDirectory);
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"전체 CSV 내보내기 실패: {e.Message}");
//             }
//         }
//
//         /// <summary>
//         /// SheetInfo를 CSV 형식으로 변환
//         /// </summary>
//         /// <param name="sheetInfo">변환할 SheetInfo 객체</param>
//         /// <returns>CSV 형식의 문자열</returns>
//         private static string ConvertSheetInfoToCSV(SheetInfo sheetInfo)
//         {
//             var csvBuilder = new StringBuilder();
//
//             foreach (var row in sheetInfo.Values)
//             {
//                 var escapedValues = new List<string>();
//
//                 foreach (var cell in row)
//                 {
//                     escapedValues.Add(EscapeCSVValue(cell));
//                 }
//
//                 csvBuilder.AppendLine(string.Join(",", escapedValues));
//             }
//
//             Debug.Log($"✅ {sheetInfo.SheetName} → CSV 변환 완료: {sheetInfo.RowCount}행");
//             return csvBuilder.ToString();
//         }
//
//         /// <summary>
//         /// CSV 셀 값을 이스케이프 처리
//         /// </summary>
//         /// <param name="value">원본 값</param>
//         /// <returns>이스케이프 처리된 값</returns>
//         private static string EscapeCSVValue(string value)
//         {
//             if (string.IsNullOrEmpty(value))
//                 return "";
//
//             // 쉼표, 따옴표, 줄바꿈이 포함된 경우 따옴표로 감싸고 내부 따옴표는 두 개로 변환
//             if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
//             {
//                 return "\"" + value.Replace("\"", "\"\"") + "\"";
//             }
//
//             return value;
//         }
//
//         /// <summary>
//         /// 폴더를 탐색기에서 열어줌
//         /// </summary>
//         /// <param name="directoryPath">폴더 경로</param>
//         private static void OpenDirectoryInExplorer(string directoryPath)
//         {
//             try
//             {
//                 if (!Directory.Exists(directoryPath))
//                 {
//                     Debug.LogWarning("폴더가 존재하지 않아 탐색기를 열 수 없습니다.");
//                     return;
//                 }
//
//                 // Windows
//                 if (Application.platform == RuntimePlatform.WindowsEditor)
//                 {
//                     System.Diagnostics.Process.Start("explorer.exe", $"\"{directoryPath.Replace("/", "\\")}\"");
//                 }
//                 // macOS
//                 else if (Application.platform == RuntimePlatform.OSXEditor)
//                 {
//                     System.Diagnostics.Process.Start("open", $"\"{directoryPath}\"");
//                 }
//                 // Linux
//                 else
//                 {
//                     System.Diagnostics.Process.Start("xdg-open", $"\"{directoryPath}\"");
//                 }
//
//                 Debug.Log($"📂 탐색기에서 폴더 열기: {directoryPath}");
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"탐색기 열기 실패: {e.Message}");
//
//                 // 대안으로 Unity의 RevealInFinder 사용
//                 try
//                 {
//                     EditorUtility.RevealInFinder(directoryPath);
//                     Debug.Log("Unity의 RevealInFinder로 대체 실행됨");
//                 }
//                 catch (Exception fallbackException)
//                 {
//                     Debug.LogError($"대체 방법도 실패: {fallbackException.Message}");
//                 }
//             }
//         }
//     }
// }
