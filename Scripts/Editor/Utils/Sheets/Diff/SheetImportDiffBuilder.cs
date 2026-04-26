using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.Utils.Sheets.Diff
{
    public static class SheetImportDiffBuilder
    {
        public static SheetImportDiffReport Build(
            IReadOnlyList<SheetInfo> sheets,
            ExportScope scope,
            ODDatabase database)
        {
            var report = new SheetImportDiffReport();
            if (sheets == null)
                return report;

            foreach (var sheet in sheets)
                report.AddSheet(BuildSheetDiff(sheet, scope, database));

            return report;
        }

        private static SheetImportSheetDiff BuildSheetDiff(
            SheetInfo sheet,
            ExportScope scope,
            ODDatabase database)
        {
            if (sheet == null)
                return new SheetImportSheetDiff(string.Empty, string.Empty, true, "Sheet is null.");

            if (!scope.All && sheet.ID != scope.TargetTableId)
                return new SheetImportSheetDiff(sheet.Name, sheet.ID, true, "Outside selected table scope.");

            if (sheet.Name != null && sheet.Name.StartsWith(SheetConfig.IGNORE_PREFIX))
                return new SheetImportSheetDiff(sheet.Name, sheet.ID, true, "Ignored sheet.");

            var table = database?.Tables.Read(new ODDBID(sheet.ID)) as Table;
            if (table == null)
                return new SheetImportSheetDiff(sheet.Name, sheet.ID, true, $"Table '{sheet.ID}' was not found.");

            if (!TryReadImportedRows(sheet, out var importedRows))
                return new SheetImportSheetDiff(sheet.Name, sheet.ID, true, $"Sheet is missing {SheetConfig.ROW_NAME_MARKER}.");

            var diff = new SheetImportSheetDiff(sheet.Name, sheet.ID);
            var currentRows = table.Rows.ToDictionary(row => row.ID.ToString(), row => row);

            foreach (var imported in importedRows)
            {
                if (!currentRows.TryGetValue(imported.RowId, out var currentRow))
                {
                    diff.AddRow(new SheetImportRowDiff(
                        SheetImportDiffKind.Added,
                        imported.RowId,
                        "Row will be added."));
                    continue;
                }

                var cellChanges = GetCellChanges(currentRow, imported.Cells);
                var changed = cellChanges.Count > 0;
                diff.AddRow(new SheetImportRowDiff(
                    changed ? SheetImportDiffKind.Updated : SheetImportDiffKind.Unchanged,
                    imported.RowId,
                    changed ? $"{cellChanges.Count} serialized cell value(s) changed." : "No serialized cell changes.",
                    cellChanges));
            }

            var importedIds = new HashSet<string>(importedRows.Select(row => row.RowId));
            foreach (var current in currentRows.Values)
            {
                var rowId = current.ID.ToString();
                if (!importedIds.Contains(rowId))
                {
                    diff.AddRow(new SheetImportRowDiff(
                        SheetImportDiffKind.Removed,
                        rowId,
                        "Row will be removed because it is absent from the import sheet."));
                }
            }

            return diff;
        }

        private static bool TryReadImportedRows(SheetInfo sheet, out List<ImportedRow> importedRows)
        {
            importedRows = new List<ImportedRow>();
            if (sheet.Values == null || sheet.Values.Count == 0)
                return false;

            var nameRow = sheet.Values[0];
            if (nameRow == null || nameRow.Count == 0 || nameRow[0] != SheetConfig.ROW_NAME_MARKER)
                return false;

            var dataStartIndex = sheet.Values.Count > 1
                && sheet.Values[1] != null
                && sheet.Values[1].Count > 0
                && sheet.Values[1][0] == SheetConfig.ROW_TYPE_MARKER
                    ? 2
                    : 1;

            var dataColumnIndices = new List<int>();
            for (var i = 2; i < nameRow.Count; i++)
            {
                var columnName = nameRow[i];
                if (!string.IsNullOrEmpty(columnName) && columnName.StartsWith(SheetConfig.IGNORE_PREFIX))
                    continue;
                dataColumnIndices.Add(i);
            }

            for (var rowIndex = dataStartIndex; rowIndex < sheet.Values.Count; rowIndex++)
            {
                var row = sheet.Values[rowIndex];
                if (row == null || row.Count == 0)
                    continue;
                if (!string.IsNullOrEmpty(row[0]) && row[0].StartsWith(SheetConfig.ROW_COMMENT_PREFIX))
                    continue;
                if (row.Count <= 1 || string.IsNullOrEmpty(row[1]))
                    continue;

                var cells = new List<ImportedCell>();
                foreach (var columnIndex in dataColumnIndices)
                {
                    var columnName = columnIndex < nameRow.Count ? nameRow[columnIndex] : $"Column {columnIndex}";
                    var value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                    cells.Add(new ImportedCell(columnName, value));
                }
                importedRows.Add(new ImportedRow(row[1], cells));
            }

            return true;
        }

        private static IReadOnlyList<SheetImportCellDiff> GetCellChanges(
            Row currentRow,
            IReadOnlyList<ImportedCell> importedCells)
        {
            var changes = new List<SheetImportCellDiff>();
            var count = System.Math.Max(currentRow.Cells.Count, importedCells.Count);
            for (var i = 0; i < count; i++)
            {
                var currentValue = i < currentRow.Cells.Count
                    ? currentRow.GetData(i)?.SerializedData ?? string.Empty
                    : string.Empty;
                var importedValue = i < importedCells.Count ? importedCells[i].Value ?? string.Empty : string.Empty;
                if (currentValue != importedValue)
                {
                    var columnName = i < importedCells.Count ? importedCells[i].ColumnName : $"Column {i + 1}";
                    changes.Add(new SheetImportCellDiff(columnName, currentValue, importedValue));
                }
            }
            return changes;
        }

        private readonly struct ImportedRow
        {
            public readonly string RowId;
            public readonly IReadOnlyList<ImportedCell> Cells;

            public ImportedRow(string rowId, IReadOnlyList<ImportedCell> cells)
            {
                RowId = rowId;
                Cells = cells;
            }
        }

        private readonly struct ImportedCell
        {
            public readonly string ColumnName;
            public readonly string Value;

            public ImportedCell(string columnName, string value)
            {
                ColumnName = columnName;
                Value = value;
            }
        }
    }
}
