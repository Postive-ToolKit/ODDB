using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using TeamODD.ODDB.Editors;
using UnityEditor;
using UnityEngine;
using TeamODD.ODDB.Editors.Utils.Sheets;
namespace Plugins.ODDB.Scripts.Editor.Utils.Sheets.CSV
{
    public class CSVUtility
    {
        public const string MENU_ROOT = ODDBEditorConst.MENU_ROOT + "CSV/";
        [MenuItem(MENU_ROOT + "Export All Sheets to CSV")]
        public static void ExportAllSheetsToCSV()
        {
            try
            {
                var sheetConverter = new ODDBSheetConverter();
                var sheetList = sheetConverter.GetAllSheets();

                if (sheetList == null || sheetList.Count == 0)
                {
                    Debug.LogWarning("There are no sheets available to export.");
                    return;
                }

                Debug.Log($"📄 Total Sheets to Export: {sheetList.Count}");
                // 저장 폴더 선택
                var baseDirectory = EditorUtility.OpenFolderPanel("Please select a folder to save CSV files", "", "");
                if (string.IsNullOrEmpty(baseDirectory))
                {
                    Debug.Log("CSV Export cancelled by user.");
                    return;
                }

                // 각 시트를 개별 CSV 파일로 저장
                var savedCount = 0;
                foreach (var sheetInfo in sheetList)
                {
                    if (sheetInfo.IsEmpty)
                        continue;

                    var csvContent = ConvertSheetInfoToCSV(sheetInfo);
                    var fileName = $"{sheetInfo.Name}_{sheetInfo.ID}.csv";
                    var filePath = Path.Combine(baseDirectory, fileName);

                    try
                    {
                        var utf8WithBom = new UTF8Encoding(true);
                        File.WriteAllText(filePath, csvContent, utf8WithBom);
                        savedCount++;

                        Debug.Log($"✅ CSV Save Success: {fileName} - {sheetInfo.RowCount} rows");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"❌ CSV Save Failed: {fileName} - {e.Message}");
                    }
                }

                Debug.Log($"🎉 CSV Export Completed: {savedCount} files saved to {baseDirectory}");

                // 저장된 폴더를 탐색기에서 열기
                OpenDirectoryInExplorer(baseDirectory);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ CSV Export Failed: {e.Message}");
            }
        }

        [MenuItem(MENU_ROOT + "Import db from CSV")]
        public static void ImportDatabaseFromCSV()
        {
            try
            {
                // Select folder containing CSV files
                var baseDirectory = EditorUtility.OpenFolderPanel("Select folder containing CSV files", "", "");
                if (string.IsNullOrEmpty(baseDirectory))
                {
                    Debug.Log("CSV Import cancelled by user.");
                    return;
                }

                // Get all CSV files from the selected directory
                var csvFiles = Directory.GetFiles(baseDirectory, "*.csv");
                if (csvFiles.Length == 0)
                {
                    Debug.LogWarning($"No CSV files found in {baseDirectory}");
                    return;
                }

                Debug.Log($"📄 Total CSV files to import: {csvFiles.Length}");

                // Convert CSV files to SheetInfo list
                var sheetList = new List<SheetInfo>();
                foreach (var csvFilePath in csvFiles)
                {
                    try
                    {
                        var sheetInfo = ConvertCSVToSheetInfo(csvFilePath);
                        if (sheetInfo != null && !sheetInfo.IsEmpty)
                        {
                            sheetList.Add(sheetInfo);
                            Debug.Log($"✅ CSV Read Success: {sheetInfo.Name} - {sheetInfo.RowCount} rows");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"❌ Failed to read CSV file {Path.GetFileName(csvFilePath)}: {e.Message}");
                    }
                }

                if (sheetList.Count == 0)
                {
                    Debug.LogWarning("No valid sheets were loaded from CSV files.");
                    return;
                }

                // Save sheets to database
                var sheetConverter = new ODDBSheetConverter();
                sheetConverter.SaveAllSheets(sheetList);

                Debug.Log($"🎉 CSV Import Completed: {sheetList.Count} sheets imported successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ CSV Import Failed: {e.Message}");
                Debug.LogException(e);
            }
        }


        private static string ConvertSheetInfoToCSV(SheetInfo sheetInfo)
        {
            var csvBuilder = new StringBuilder();

            foreach (var row in sheetInfo.Values)
            {
                var escapedValues = new List<string>();

                foreach (var cell in row)
                {
                    escapedValues.Add(EscapeCSVValue(cell));
                }

                csvBuilder.AppendLine(string.Join(",", escapedValues));
            }

            Debug.Log($"✅ CSV Conversion Success: {sheetInfo.Name} - {sheetInfo.RowCount} rows");
            return csvBuilder.ToString();
        }

        private static SheetInfo ConvertCSVToSheetInfo(string csvFilePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(csvFilePath);
            
            // Extract SheetName and SheetId from filename (format: SheetName_SheetId.csv)
            var parts = fileName.Split('_');
            if (parts.Length < 2)
            {
                Debug.LogWarning($"Invalid CSV filename format: {fileName}. Expected format: SheetName_SheetId.csv");
                return null;
            }

            var sheetName = string.Join("_", parts, 0, parts.Length - 1);
            var sheetId = parts[parts.Length - 1];

            // Read CSV content
            var csvContent = File.ReadAllText(csvFilePath, Encoding.UTF8);
            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var sheetInfo = new SheetInfo(sheetName, sheetId);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var row = ParseCSVLine(line);
                sheetInfo.Values.Add(row);
            }

            return sheetInfo;
        }

        private static List<string> ParseCSVLine(string line)
        {
            var result = new List<string>();
            var currentValue = new StringBuilder();
            var insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentValue.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        // Toggle quote mode
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (c == ',' && !insideQuotes)
                {
                    // End of field
                    result.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            // Add last field
            result.Add(currentValue.ToString());

            return result;
        }

        private static string EscapeCSVValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
        
        private static void OpenDirectoryInExplorer(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return;
                
                if (Application.platform == RuntimePlatform.WindowsEditor)
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{directoryPath.Replace("/", "\\")}\"");
                else if (Application.platform == RuntimePlatform.OSXEditor)
                    System.Diagnostics.Process.Start("open", $"\"{directoryPath}\"");
                else
                    System.Diagnostics.Process.Start("xdg-open", $"\"{directoryPath}\"");

                Debug.Log($"📂 Trying to open directory: {directoryPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to open directory: {e.Message}");
                try
                {
                    EditorUtility.RevealInFinder(directoryPath);
                }
                catch (Exception fallbackException)
                {
                    Debug.LogError($"Fallback failed to open directory: {fallbackException.Message}");
                }
            }
        }
    }
}
