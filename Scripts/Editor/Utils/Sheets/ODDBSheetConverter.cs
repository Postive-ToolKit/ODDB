// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using Newtonsoft.Json;
// using TeamODD.ODDB.Runtime.Data;
// using TeamODD.ODDB.Runtime.Data.DTO;
// using TeamODD.ODDB.Runtime.Data.Enum;
// using TeamODD.ODDB.Runtime.Settings;
// using TeamODD.ODDB.Runtime.Utils;
// using UnityEngine;
//
// namespace Plugins.ODDB.Scripts.Editor.Utils.Sheets
// {
//     public class ODDBSheetConverter
//     {
//         public const string DEFAULT_VIEW_SHEET_NAME = "Views";
//         public static readonly string[] DEFAULT_TABLEMETA_HEADERS = { "Table Meta", "ID", "Name", "Type" };
//         public const string DEFAULT_ID_COLUNM_HEADER = "ID";
//         private ODDatabaseDTO _databaseDto;
//
//         public ODDBSheetConverter()
//         {
//             _databaseDto = LoadDatabaseFromFile();
//             if (_databaseDto == null)
//                 Debug.LogError("Failed to load Database DTO");
//         }
//         
//
//         /// <summary>
//         /// Load ODDatabaseDTO from database file
//         /// </summary>
//         private ODDatabaseDTO LoadDatabaseFromFile()
//         {
//             try
//             {
//                 var fullPath = Path.Combine(ODDBSettings.Setting.Path, ODDBSettings.Setting.DBName);
//                 
//                 if (!File.Exists(fullPath))
//                 {
//                     Debug.LogError($"ODDB file not found: {fullPath}");
//                     return null;
//                 }
//
//                 var jsonContent = File.ReadAllText(fullPath);
//                 var databaseDto = JsonConvert.DeserializeObject<ODDatabaseDTO>(jsonContent);
//                 
//                 Debug.Log($"✅ Database DTO loaded successfully: {fullPath}");
//                 return databaseDto;
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Failed to load Database DTO: {e.Message}");
//                 return null;
//             }
//         }
//         
//         /// <summary>
//         /// Check if a row is empty
//         /// </summary>
//         private bool IsEmptyRow(List<string> row)
//         {
//             return row.Count == 0 || row.TrueForAll(string.IsNullOrEmpty);
//         }
//
//         /// <summary>
//         /// Convert ODDB file to complete SheetInfo list (Views + Tables)
//         /// </summary>
//         /// <returns>List of SheetInfo containing all sheet data</returns>
//         public List<SheetInfo> ConvertODDBToSheetInfoList()
//         {
//             var sheetInfoList = new List<SheetInfo>();
//             
//             // Load database file
//             var databaseDto = LoadDatabaseFromFile();
//             if (databaseDto == null)
//             {
//                 Debug.LogError("Failed to load Database DTO");
//                 return sheetInfoList;
//             }
//
//             // Add Views sheet (includes View + Table metadata)
//             var viewsSheetInfo = ConvertViewsToSheetInfo(databaseDto.ViewRepoData, databaseDto.TableRepoData);
//             if (!viewsSheetInfo.IsEmpty)
//             {
//                 sheetInfoList.Add(viewsSheetInfo);
//             }
//
//             // Add Table sheets (data only, one sheet per table)
//             var tablesSheetInfos = ConvertTableDataToSheet(databaseDto.TableRepoData);
//             sheetInfoList.AddRange(tablesSheetInfos);
//
//             Debug.Log($"✅ Total {sheetInfoList.Count} sheets converted successfully");
//             return sheetInfoList;
//         }
//
//         /// <summary>
//         /// Convert Views data to SheetInfo (includes Table metadata)
//         /// </summary>
//         private SheetInfo ConvertViewsToSheetInfo(List<ODDBViewDTO> viewDtoList, List<ODDBTableDTO> tableDtoList = null)
//         {
//             var sheetName = "Views";
//             
//             if (viewDtoList == null || viewDtoList.Count == 0)
//             {
//                 Debug.LogWarning("No Views data available");
//                 return new SheetInfo(sheetName);
//             }
//
//             // Convert both ODDBViewDTO and TableDTO lists to sheet data
//             var sheetData = CreateMetaSheet(viewDtoList, tableDtoList);
//             
//             Debug.Log($"✅ Views sheet conversion completed (includes Table metadata): {viewDtoList.Count} Views → {sheetData.Count} rows");
//             return new SheetInfo(sheetName, sheetData);
//         }
//
//         /// <summary>
//         /// Create metadata sheet using both View and Table DTOs
//         /// </summary>
//         private List<List<string>> CreateMetaSheet(List<ODDBViewDTO> viewDtoList, List<ODDBTableDTO> tableDtoList)
//         {
//             var sheetData = new List<List<string>>();
//
//             // Process View DTOs
//             foreach (var viewDto in viewDtoList)
//             {
//                 // Add metadata header for View
//                 sheetData.Add(CreateMetaHeader(viewDto).ToList());
//                 
//                 // Add View's Table Meta header
//                 sheetData.Add(DEFAULT_TABLEMETA_HEADERS.ToList());
//
//                 // Add View's Table Meta data rows
//                 if (viewDto.TableMetas != null)
//                 {
//                     foreach (var tableMeta in viewDto.TableMetas)
//                     {
//                         sheetData.Add(CreateMetaRow(tableMeta).ToList());
//                     }
//                 }
//
//                 // Add padding row for View separation
//                 sheetData.Add(new List<string>());
//
//                 Debug.Log($"✅ View sheet data converted: {viewDto.Name} ({viewDto.TableMetas?.Count ?? 0} View fields)");
//             }
//             
//             // Process Table DTOs if provided
//             if (tableDtoList != null)
//             {
//                 foreach (var tableDto in tableDtoList)
//                 {
//                     // Add metadata header for Table
//                     sheetData.Add(CreateMetaHeader(tableDto).ToList());
//                     
//                     // Add Table Meta header
//                     sheetData.Add(DEFAULT_TABLEMETA_HEADERS.ToList());
//
//                     // Add Table's Table Meta data rows
//                     if (tableDto.TableMetas != null)
//                     {
//                         foreach (var tableMeta in tableDto.TableMetas)
//                         {
//                             sheetData.Add(CreateMetaRow(tableMeta).ToList());
//                         }
//                     }
//
//                     // Add padding row for Table separation
//                     sheetData.Add(new List<string>());
//
//                     Debug.Log($"✅ Table sheet data converted: {tableDto.Name} ({tableDto.TableMetas?.Count ?? 0} Table fields)");
//                 }
//             }
//
//             // Remove last empty row if it exists
//             if (sheetData.Count > 0 && IsEmptyRow(sheetData[^1]))
//             {
//                 sheetData.RemoveAt(sheetData.Count - 1);
//             }
//
//             return sheetData;
//         }
//
//         /// <summary>
//         /// Convert Tables data to individual SheetInfo list
//         /// </summary>
//         private List<SheetInfo> ConvertTableDataToSheet(List<ODDBTableDTO> tableDtoList)
//         {
//             var sheetInfoList = new List<SheetInfo>();
//             
//             try
//             {
//                 if (tableDtoList == null || tableDtoList.Count == 0)
//                 {
//                     Debug.LogWarning("TableRepoData is empty");
//                     return sheetInfoList;
//                 }
//
//                 // Convert each table to individual sheet
//                 foreach (var tableDto in tableDtoList)
//                 {
//                     if (tableDto == null)
//                         continue;
//
//                     try
//                     {
//                         var tableSheetInfo = ConvertTableDtoToSheetInfo(tableDto);
//                         if (!tableSheetInfo.IsEmpty)
//                         {
//                             sheetInfoList.Add(tableSheetInfo);
//                             Debug.Log($"✅ Table sheet conversion completed: {tableSheetInfo.SheetName} ({tableSheetInfo.RowCount} rows)");
//                         }
//                     }
//                     catch (Exception e)
//                     {
//                         Debug.LogError($"Failed to convert individual Table DTO: {e.Message}");
//                     }
//                 }
//
//                 Debug.Log($"✅ Tables → SheetInfo conversion completed: {sheetInfoList.Count} tables");
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Failed to convert TableRepoData: {e.Message}");
//             }
//
//             return sheetInfoList;
//         }
//
//         /// <summary>
//         /// Convert individual ODDBTableDTO to SheetInfo - includes table info and data
//         /// </summary>
//         private SheetInfo ConvertTableDtoToSheetInfo(ODDBTableDTO tableDto)
//         {
//             var sheetName = $"Table_{tableDto.Name ?? tableDto.ID ?? "Unknown"}";
//             var sheetData = new List<List<string>>();
//
//             try
//             {
//                 sheetData.Add(CreateTableColumnHeader(tableDto).ToList());
//
//                 // Add actual table data rows (Data is string[][] format)
//                 if (tableDto.Data != null && tableDto.Data.Length > 0)
//                 {
//                     foreach (var dataRow in tableDto.Data)
//                     {
//                         var rowValues = new List<string>();
//                         
//                         // Convert string[] array to List<string>
//                         if (dataRow != null)
//                         {
//                             rowValues.AddRange(dataRow);
//                         }
//                         
//                         // Fill with empty strings if column count is insufficient
//                         while (rowValues.Count < (tableDto.TableMetas?.Count ?? 0))
//                         {
//                             rowValues.Add("");
//                         }
//                         
//                         sheetData.Add(rowValues);
//                     }
//                 }
//
//                 Debug.Log($"✅ Table DTO → SheetInfo conversion (with table info): {sheetName} " +
//                          $"({tableDto.TableMetas?.Count ?? 0} columns, {tableDto.Data?.Length ?? 0} data rows)");
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Failed to convert Table DTO: {e.Message}");
//                 return new SheetInfo(sheetName);
//             }
//
//             return new SheetInfo(sheetName, sheetData);
//         }
//         
//         /// <summary>
//         /// Create column header row for Table data
//         /// </summary>
//         /// <param name="viewDto"> The View DTO containing TableMetas</param>
//         /// <returns> Array of column header names </returns>
//         private string[] CreateTableColumnHeader(ODDBViewDTO viewDto)
//         {
//             var result = new List<string>();
//             result.Add(DEFAULT_ID_COLUNM_HEADER);
//             if (!string.IsNullOrEmpty(viewDto.ID))
//             {
//                 var parentViewDto = _databaseDto.ViewRepoData?.FirstOrDefault(v => v.ID == viewDto.ParentView);
//                 if (parentViewDto != null)
//                 {
//                     var parentHeaders = CreateTableColumnHeader(parentViewDto);
//                     parentHeaders = parentHeaders.Skip(1).ToArray(); // Skip ID column to avoid duplication
//                     result.AddRange(parentHeaders);
//                 }
//                     
//             }
//             
//             if (viewDto.TableMetas != null)
//                 result.AddRange(viewDto.TableMetas.Select(meta => meta.Name ?? ""));
//             return result.ToArray();
//         }
//
//         /// <summary>
//         /// Create metadata header for View or Table
//         /// </summary>
//         private string[] CreateMetaHeader(ODDBViewDTO viewDto)
//         {
//             var metaType = viewDto is ODDBTableDTO ? "Table" : "View";
//             return new[]
//             {
//                 metaType,
//                 viewDto.ID ?? "",
//                 viewDto.Name ?? "",
//                 viewDto.BindType ?? "",
//                 viewDto.ParentView ?? ""
//             };
//         }
//         
//         /// <summary>
//         /// Create metadata row for field
//         /// </summary>
//         private string[] CreateMetaRow(ODDBField field)
//         {
//             return new[]
//             {
//                 "",
//                 field.ID?.ToString() ?? "",
//                 field.Name ?? "",
//                 field.Type.ToString()
//             };
//         }
//
//         /// <summary>
//         /// Convert SheetInfo list to ODDatabaseDTO
//         /// </summary>
//         /// <param name="sheetInfoList">List of SheetInfo to convert</param>
//         /// <returns>ODDatabaseDTO containing parsed data</returns>
//         public ODDatabaseDTO ConvertSheetInfoListToDatabase(List<SheetInfo> sheetInfoList)
//         {
//             try
//             {
//                 var viewDtos = new List<ODDBViewDTO>();
//                 var tableDtos = new List<ODDBTableDTO>();
//
//                 foreach (var sheetInfo in sheetInfoList)
//                 {
//                     if (sheetInfo == null || sheetInfo.IsEmpty)
//                         continue;
//
//                     if (sheetInfo.SheetName.Equals("Views", StringComparison.OrdinalIgnoreCase))
//                     {
//                         // Parse Views sheet containing metadata
//                         var parsedData = ParseViewsSheet(sheetInfo);
//                         viewDtos.AddRange(parsedData.Views);
//                         tableDtos.AddRange(parsedData.Tables);
//                     }
//                     else if (sheetInfo.SheetName.StartsWith("Table_", StringComparison.OrdinalIgnoreCase))
//                     {
//                         // Parse Table sheet containing data
//                         var tableDto = ParseTableDataSheet(sheetInfo);
//                         if (tableDto != null)
//                         {
//                             // Find existing table DTO and update its data
//                             var existingTable = tableDtos.FirstOrDefault(t => 
//                                 t.Name?.Equals(tableDto.Name, StringComparison.OrdinalIgnoreCase) == true ||
//                                 t.ID?.Equals(tableDto.ID, StringComparison.OrdinalIgnoreCase) == true);
//                             
//                             if (existingTable != null)
//                             {
//                                 existingTable.Data = tableDto.Data;
//                                 // Merge metadata if needed
//                                 if (existingTable.TableMetas == null || existingTable.TableMetas.Count == 0)
//                                 {
//                                     existingTable.TableMetas = tableDto.TableMetas;
//                                 }
//                             }
//                             else
//                             {
//                                 tableDtos.Add(tableDto);
//                             }
//                         }
//                     }
//                 }
//
//                 var databaseDto = new ODDatabaseDTO(tableDtos, viewDtos);
//                 Debug.Log($"✅ Converted SheetInfo to DatabaseDTO: {viewDtos.Count} Views, {tableDtos.Count} Tables");
//                 
//                 return databaseDto;
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Failed to convert SheetInfo list to DatabaseDTO: {e.Message}");
//                 return null;
//             }
//         }
//
//         /// <summary>
//         /// Parse Views sheet containing metadata
//         /// </summary>
//         /// <param name="sheetInfo">Views SheetInfo</param>
//         /// <returns>Tuple containing parsed Views and Tables</returns>
//         private (List<ODDBViewDTO> Views, List<ODDBTableDTO> Tables) ParseViewsSheet(SheetInfo sheetInfo)
//         {
//             var views = new List<ODDBViewDTO>();
//             var tables = new List<ODDBTableDTO>();
//
//             try
//             {
//                 var rows = sheetInfo.Values;
//                 var currentIndex = 0;
//
//                 while (currentIndex < rows.Count)
//                 {
//                     var row = rows[currentIndex];
//                     
//                     if (row == null || row.Count == 0 || string.IsNullOrEmpty(row[0]))
//                     {
//                         currentIndex++;
//                         continue;
//                     }
//
//                     if (row[0].Equals("View", StringComparison.OrdinalIgnoreCase))
//                     {
//                         var viewData = ParseViewMetadataFromRows(rows, ref currentIndex);
//                         if (viewData != null)
//                             views.Add(viewData);
//                     }
//                     else if (row[0].Equals("Table", StringComparison.OrdinalIgnoreCase))
//                     {
//                         var tableData = ParseTableMetadataFromRows(rows, ref currentIndex);
//                         if (tableData != null)
//                             tables.Add(tableData);
//                     }
//                     else
//                     {
//                         currentIndex++;
//                     }
//                 }
//
//                 Debug.Log($"✅ Parsed Views sheet: {views.Count} Views, {tables.Count} Tables");
//                 return (views, tables);
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Failed to parse Views sheet: {e.Message}");
//                 return (new List<ODDBViewDTO>(), new List<ODDBTableDTO>());
//             }
//         }
//
//         /// <summary>
//         /// Parse View metadata from sheet rows
//         /// </summary>
//         /// <param name="rows">All sheet rows</param>
//         /// <param name="currentIndex">Current row index (will be modified)</param>
//         /// <returns>Parsed ODDBViewDTO</returns>
//         private ODDBViewDTO ParseViewMetadataFromRows(List<List<string>> rows, ref int currentIndex)
//         {
//             try
//             {
//                 var viewRow = rows[currentIndex];
//                 if (viewRow.Count < 5) return null;
//
//                 var viewDto = new ODDBViewDTO
//                 {
//                     ID = GetSafeValue(viewRow, 1),
//                     Name = GetSafeValue(viewRow, 2),
//                     BindType = GetSafeValue(viewRow, 3),
//                     ParentView = GetSafeValue(viewRow, 4),
//                     TableMetas = new List<ODDBField>()
//                 };
//
//                 currentIndex++; // Move to next row
//
//                 // Parse Table Meta header and data
//                 if (currentIndex < rows.Count && 
//                     rows[currentIndex].Count > 0 &&
//                     rows[currentIndex][0].Equals("Table Meta", StringComparison.OrdinalIgnoreCase))
//                 {
//                     currentIndex++; // Skip header
//
//                     // Parse field data
//                     while (currentIndex < rows.Count)
//                     {
//                         var fieldRow = rows[currentIndex];
//                         if (fieldRow.Count == 0 || !string.IsNullOrEmpty(fieldRow[0]))
//                             break; // End of this section
//
//                         if (fieldRow.Count >= 4)
//                         {
//                             var field = new ODDBField
//                             {
//                                 ID = new ODDBID(GetSafeValue(fieldRow, 1)),
//                                 Name = GetSafeValue(fieldRow, 2),
//                                 Type = Enum.TryParse<ODDBDataType>(GetSafeValue(fieldRow, 3), out var type) ? type : ODDBDataType.String
//                             };
//                             viewDto.TableMetas.Add(field);
//                         }
//                         currentIndex++;
//                     }
//                 }
//
//                 return viewDto;
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Failed to parse View metadata: {e.Message}");
//                 return null;
//             }
//         }
//
//         /// <summary>
//         /// Parse Table metadata from sheet rows
//         /// </summary>
//         /// <param name="rows">All sheet rows</param>
//         /// <param name="currentIndex">Current row index (will be modified)</param>
//         /// <returns>Parsed ODDBTableDTO</returns>
//         private ODDBTableDTO ParseTableMetadataFromRows(List<List<string>> rows, ref int currentIndex)
//         {
//             try
//             {
//                 var tableRow = rows[currentIndex];
//                 if (tableRow.Count < 5) return null;
//
//                 var tableDto = new ODDBTableDTO
//                 {
//                     ID = GetSafeValue(tableRow, 1),
//                     Name = GetSafeValue(tableRow, 2),
//                     BindType = GetSafeValue(tableRow, 3),
//                     ParentView = GetSafeValue(tableRow, 4),
//                     TableMetas = new List<ODDBField>(),
//                     Data = new string[0][]
//                 };
//
//                 currentIndex++; // Move to next row
//
//                 // Parse Table Meta header and data
//                 if (currentIndex < rows.Count && 
//                     rows[currentIndex].Count > 0 &&
//                     rows[currentIndex][0].Equals("Table Meta", StringComparison.OrdinalIgnoreCase))
//                 {
//                     currentIndex++; // Skip header
//
//                     // Parse field data
//                     while (currentIndex < rows.Count)
//                     {
//                         var fieldRow = rows[currentIndex];
//                         if (fieldRow.Count == 0 || !string.IsNullOrEmpty(fieldRow[0]))
//                             break; // End of this section
//
//                         if (fieldRow.Count >= 4)
//                         {
//                             var field = new ODDBField
//                             {
//                                 ID = new ODDBID(GetSafeValue(fieldRow, 1)),
//                                 Name = GetSafeValue(fieldRow, 2),
//                                 Type = Enum.TryParse<ODDBDataType>(GetSafeValue(fieldRow, 3), out var type) ? type : ODDBDataType.String
//                             };
//                             tableDto.TableMetas.Add(field);
//                         }
//                         currentIndex++;
//                     }
//                 }
//
//                 return tableDto;
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Failed to parse Table metadata: {e.Message}");
//                 return null;
//             }
//         }
//
//         /// <summary>
//         /// Parse Table data sheet
//         /// </summary>
//         /// <param name="sheetInfo">Table SheetInfo</param>
//         /// <returns>Parsed ODDBTableDTO with data</returns>
//         private ODDBTableDTO ParseTableDataSheet(SheetInfo sheetInfo)
//         {
//             try
//             {
//                 var rows = sheetInfo.Values;
//                 if (rows.Count == 0) return null;
//
//                 var tableDto = new ODDBTableDTO();
//                 var currentIndex = 0;
//
//                 // Check if first row contains table info
//                 var firstRow = rows[0];
//                 if (firstRow.Count >= 5 && firstRow[0].Equals("Table", StringComparison.OrdinalIgnoreCase))
//                 {
//                     tableDto.ID = GetSafeValue(firstRow, 1);
//                     tableDto.Name = GetSafeValue(firstRow, 2);
//                     tableDto.BindType = GetSafeValue(firstRow, 3);
//                     tableDto.ParentView = GetSafeValue(firstRow, 4);
//                     currentIndex = 2; // Skip table info and empty line
//                 }
//
//                 // Parse column headers
//                 if (currentIndex < rows.Count)
//                 {
//                     var columnHeaders = rows[currentIndex];
//                     tableDto.TableMetas = new List<ODDBField>();
//                     
//                     for (int i = 0; i < columnHeaders.Count; i++)
//                     {
//                         tableDto.TableMetas.Add(new ODDBField
//                         {
//                             ID = i,
//                             Name = GetSafeValue(columnHeaders, i),
//                             Type = ODDBDataType.String // Default type
//                         });
//                     }
//                     currentIndex++;
//                 }
//
//                 // Parse data rows
//                 var dataRows = new List<string[]>();
//                 for (int i = currentIndex; i < rows.Count; i++)
//                 {
//                     var row = rows[i];
//                     if (row.Count == 0 || row.TrueForAll(string.IsNullOrEmpty))
//                         continue;
//
//                     var rowData = new string[tableDto.TableMetas?.Count ?? row.Count];
//                     for (int j = 0; j < rowData.Length; j++)
//                     {
//                         rowData[j] = GetSafeValue(row, j);
//                     }
//                     dataRows.Add(rowData);
//                 }
//
//                 tableDto.Data = dataRows.ToArray();
//
//                 Debug.Log($"✅ Parsed Table sheet: {tableDto.Name} ({tableDto.TableMetas?.Count} columns, {tableDto.Data?.Length} rows)");
//                 return tableDto;
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Failed to parse Table data sheet: {e.Message}");
//                 return null;
//             }
//         }
//
//         /// <summary>
//         /// Safely get value from list at specified index
//         /// </summary>
//         /// <param name="list">Source list</param>
//         /// <param name="index">Target index</param>
//         /// <returns>Value at index or empty string if out of bounds</returns>
//         private string GetSafeValue(List<string> list, int index)
//         {
//             return index < list.Count ? (list[index] ?? "") : "";
//         }
//
//         /// <summary>
//         /// Convert SheetInfo list to JSON string
//         /// </summary>
//         /// <param name="sheetInfoList">List of SheetInfo to convert</param>
//         /// <returns>JSON string representation of the database</returns>
//         public string ConvertSheetInfoListToJson(List<SheetInfo> sheetInfoList)
//         {
//             try
//             {
//                 var databaseDto = ConvertSheetInfoListToDatabase(sheetInfoList);
//                 if (databaseDto == null)
//                 {
//                     Debug.LogError("Failed to convert SheetInfo to DatabaseDTO");
//                     return null;
//                 }
//
//                 var jsonString = JsonConvert.SerializeObject(databaseDto, Formatting.Indented);
//                 Debug.Log($"✅ Converted SheetInfo to JSON: {jsonString.Length} characters");
//                 
//                 return jsonString;
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Failed to convert SheetInfo to JSON: {e.Message}");
//                 return null;
//             }
//         }
//     }
// }
