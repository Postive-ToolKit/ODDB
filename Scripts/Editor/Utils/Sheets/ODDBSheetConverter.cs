using System;
using System.Collections.Generic;
using System.IO;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Utils.Sheets
{
    public class ODDBSheetConverter
    {
        private ODDatabase _dB;

        public ODDBSheetConverter()
        {
            _dB = LoadDatabaseFromFile();
            if (_dB == null)
                Debug.LogError("Failed to load Database");
        }

        /// <summary>
        /// Load ODDatabaseDTO from database file
        /// </summary>
        private ODDatabase LoadDatabaseFromFile()
        {
            var fullPath = Path.Combine(ODDBSettings.Setting.Path, ODDBSettings.Setting.DBName);
            var dataService = new ODDBDataService();
            if (dataService.LoadDatabase(fullPath, out var database) == false)
            {
                Debug.LogError("Failed to load database");
                return null;
            }
            return database;
        }

        private void SaveDatabaseToFile(ODDatabase database)
        {
            var fullPath = Path.Combine(ODDBSettings.Setting.Path, ODDBSettings.Setting.DBName);
            var dataService = new ODDBDataService();
            if (dataService.SaveDatabase(database, fullPath) == false)
            {
                Debug.LogError("Failed to save database");
            }
        }
        
        public void SaveAllSheets(List<SheetInfo> sheets)
        {
            foreach (var sheet in sheets)
            {
                if (sheet.Name.StartsWith(SheetConfig.IGNORE_PREFIX))
                    continue;
                
                var table = _dB.Tables.Read(new ODDBID(sheet.ID)) as Table;
                if (table == null)
                {
                    Debug.LogError($"Table with ID {sheet.ID} not found.");
                    continue;
                }

                // Clear existing rows
                table.Clear();

                // Skip header row and populate data rows
                for (int i = 1; i < sheet.Values.Count; i++)
                {
                    var rowData = sheet.Values[i];
                    
                    // First element is the Row ID
                    var rowId = new ODDBID(rowData[0]);
                    var newRow = table.AddRow();
                    newRow.ID = rowId;
                    // Populate cell data (skip the first element which is the ID)
                    for (int j = 1; j < rowData.Count; j++)
                    {
                        var cellData = rowData[j];
                        newRow.SetData(j - 1, cellData, true);
                    }
                }
            }
            SaveDatabaseToFile(_dB);
        }
        
        public List<SheetInfo> GetAllSheets()
        {
            var sheets = new List<SheetInfo>();
            foreach (var view in _dB.Tables.GetAll())
            {
                if (view is not Table table)
                    continue;
                sheets.Add(CreateSheetsFromTable(table));
            }
            return sheets;
        }

        /// <summary>
        /// Converts a single <see cref="Table"/> into a <see cref="SheetInfo"/>.
        /// Delegates to the same internal builder used by <see cref="GetAllSheets"/>
        /// so byte-identical output is guaranteed for per-table exports.
        /// </summary>
        public SheetInfo ExportTable(Table table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            return CreateSheetsFromTable(table);
        }

        /// <summary>
        /// Writes a single <see cref="SheetInfo"/> back into the given <paramref name="table"/>,
        /// replacing its rows in place. Caller is responsible for persisting the database after
        /// the sheet has been applied (see <see cref="ODDBDataService"/> or UseCase.SaveDatabase).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the sheet's ID does not match the target table's ID.
        /// </exception>
        public void ApplySheetToTable(Table table, SheetInfo sheet)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (sheet.Name != null && sheet.Name.StartsWith(SheetConfig.IGNORE_PREFIX))
                return;

            string tableIdStr = table.ID;
            if (!string.Equals(tableIdStr, sheet.ID))
                throw new InvalidOperationException(
                    $"Sheet id '{sheet.ID}' does not match target table id '{tableIdStr}'.");

            table.Clear();

            for (int i = 1; i < sheet.Values.Count; i++)
            {
                var rowData = sheet.Values[i];
                if (rowData == null || rowData.Count == 0) continue;

                var newRow = table.AddRow();
                newRow.ID = new ODDBID(rowData[0]);
                for (int j = 1; j < rowData.Count; j++)
                {
                    newRow.SetData(j - 1, rowData[j], true);
                }
            }
        }

        private SheetInfo CreateSheetsFromTable(Table table)
        {
            var sheet = new SheetInfo
            {
                Name = table.Name,
                ID = table.ID,
            };

            // Create header row with "ID" in the first column, followed by field names
            var headerRow = new List<string> { "ID" };
            foreach (var field in table.TotalFields)
                headerRow.Add(field.Name);
            sheet.Values.Add(headerRow);

            // Add data rows
            foreach (var rowData in table.Rows)
            {
                var row = new List<string>();
                // First element is the Row ID
                row.Add(rowData.ID);
                    
                // Add cell data (skip the first element which is the ID)
                for (int i = 0; i < table.TotalFields.Count; i++)
                {
                    var cellData = rowData.GetData(i);
                    row.Add(cellData.SerializedData);
                }
                sheet.Values.Add(row);
            }
            return sheet;
        }
    }
}
