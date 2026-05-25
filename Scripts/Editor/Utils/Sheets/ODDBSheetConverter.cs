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

        public ODDBSheetConverter(ODDatabase database)
        {
            _dB = database ?? throw new ArgumentNullException(nameof(database));
        }

        private readonly struct SheetHeaderInfo
        {
            public readonly int DataStartIndex;
            public readonly int IdColumnIndex;
            public readonly IReadOnlyList<int> DataColumnIndices;

            public SheetHeaderInfo(int dataStartIndex, int idColumnIndex, IReadOnlyList<int> dataColumnIndices)
            {
                DataStartIndex = dataStartIndex;
                IdColumnIndex = idColumnIndex;
                DataColumnIndices = dataColumnIndices;
            }
        }

        private static bool TryParseSheetHeader(SheetInfo sheet, out SheetHeaderInfo headerInfo)
        {
            headerInfo = default;
            if (sheet.Values.Count == 0) return false;

            var nameRow = sheet.Values[0];
            if (nameRow.Count == 0) return false;
            if (nameRow[0] != SheetConfig.ROW_NAME_MARKER) return false;

            var dataStartIndex = sheet.Values.Count > 1
                && sheet.Values[1].Count > 0
                && sheet.Values[1][0] == SheetConfig.ROW_TYPE_MARKER
                    ? 2
                    : 1;

            var dataColumnIndices = new List<int>();
            for (int i = 2; i < nameRow.Count; i++)
            {
                var columnName = nameRow[i];
                if (!string.IsNullOrEmpty(columnName) && columnName.StartsWith(SheetConfig.IGNORE_PREFIX))
                    continue;
                dataColumnIndices.Add(i);
            }

            headerInfo = new SheetHeaderInfo(dataStartIndex, 1, dataColumnIndices);
            return true;
        }

        private static bool IsCommentRow(List<string> row)
        {
            if (row == null || row.Count == 0) return true;
            var firstCell = row[0];
            return !string.IsNullOrEmpty(firstCell)
                && firstCell.StartsWith(SheetConfig.ROW_COMMENT_PREFIX);
        }

        private static void ApplyRowDataToTable(Table table, List<string> rowData, SheetHeaderInfo headerInfo)
        {
            if (rowData.Count <= headerInfo.IdColumnIndex)
                return;

            var rowId = rowData[headerInfo.IdColumnIndex];
            if (string.IsNullOrEmpty(rowId))
                return;

            if (table.GetRow(rowId) != null)
            {
                Debug.LogWarning($"Duplicate row ID found during sheet import: {rowId}. Skipping this row.");
                return;
            }

            var newRow = table.AddRow(new ODDBID(rowId));

            for (int fieldIndex = 0; fieldIndex < headerInfo.DataColumnIndices.Count; fieldIndex++)
            {
                var columnIndex = headerInfo.DataColumnIndices[fieldIndex];
                if (columnIndex < rowData.Count)
                    newRow.SetData(fieldIndex, rowData[columnIndex], true);
            }
        }

        /// <summary>
        /// Load ODDatabaseDTO from database file
        /// </summary>
        private static ODDatabase LoadDatabaseFromFile()
        {
            var useCase = TeamODD.ODDB.Editors.ODDBEditorRuntime.UseCase;
            if (useCase?.DataBase is ODDatabase shared)
                return shared;

            var fullPath = Path.Combine(ODDBRuntimeSettings.Setting.Path, ODDBRuntimeSettings.Setting.DBName);
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
            var useCase = TeamODD.ODDB.Editors.ODDBEditorRuntime.UseCase;
            if (useCase?.DataBase == database)
            {
                var fullPath = Path.Combine(ODDBRuntimeSettings.Setting.Path, ODDBRuntimeSettings.Setting.DBName);
                useCase.SaveDatabase(fullPath);
                return;
            }

            var path = Path.Combine(ODDBRuntimeSettings.Setting.Path, ODDBRuntimeSettings.Setting.DBName);
            var dataService = new ODDBDataService();
            if (dataService.SaveDatabase(database, path) == false)
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

                ApplySheetToTable(table, sheet);
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

            if (!TryParseSheetHeader(sheet, out var headerInfo))
                return;

            table.Clear();

            for (int i = headerInfo.DataStartIndex; i < sheet.Values.Count; i++)
            {
                var rowData = sheet.Values[i];
                if (rowData == null || rowData.Count == 0) continue;
                if (IsCommentRow(rowData)) continue;
                ApplyRowDataToTable(table, rowData, headerInfo);
            }
        }

        private SheetInfo CreateSheetsFromTable(Table table)
        {
            var sheet = new SheetInfo
            {
                Name = table.Name,
                ID = table.ID,
            };

            var nameRow = new List<string> { SheetConfig.ROW_NAME_MARKER, "ID" };
            foreach (var field in table.TotalFields)
                nameRow.Add(field.Name);
            sheet.Values.Add(nameRow);

            var typeRow = new List<string> { SheetConfig.ROW_TYPE_MARKER, "ID" };
            foreach (var field in table.TotalFields)
                typeRow.Add(field.Type.ToString());
            sheet.Values.Add(typeRow);

            foreach (var rowData in table.Rows)
            {
                var row = new List<string>();
                row.Add("");
                row.Add(rowData.ID);
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
