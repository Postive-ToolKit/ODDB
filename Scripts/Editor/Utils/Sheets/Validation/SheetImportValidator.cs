using System.Collections.Generic;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.Utils.Sheets.Validation
{
    public static class SheetImportValidator
    {
        public static SheetValidationReport Validate(
            IReadOnlyList<SheetInfo> sheets,
            ExportScope scope,
            ODDatabase database)
        {
            var report = new SheetValidationReport();
            if (sheets == null)
            {
                report.Add(new SheetValidationIssue(
                    SheetValidationSeverity.Error,
                    string.Empty,
                    string.Empty,
                    -1,
                    "Sheet list is null."));
                return report;
            }

            for (var i = 0; i < sheets.Count; i++)
                ValidateSheet(sheets[i], scope, database, report);

            return report;
        }

        private static void ValidateSheet(
            SheetInfo sheet,
            ExportScope scope,
            ODDatabase database,
            SheetValidationReport report)
        {
            if (sheet == null)
            {
                report.Add(new SheetValidationIssue(
                    SheetValidationSeverity.Error,
                    string.Empty,
                    string.Empty,
                    -1,
                    "Sheet is null."));
                return;
            }

            if (!scope.All && sheet.ID != scope.TargetTableId)
                return;

            if (sheet.Name != null && sheet.Name.StartsWith(SheetConfig.IGNORE_PREFIX))
                return;

            if (string.IsNullOrEmpty(sheet.ID))
            {
                report.Add(new SheetValidationIssue(
                    SheetValidationSeverity.Error,
                    sheet.Name,
                    sheet.ID,
                    -1,
                    "Sheet has an empty sheet ID."));
            }
            else if (database != null && database.Tables.Read(new ODDBID(sheet.ID)) == null)
            {
                report.Add(new SheetValidationIssue(
                    SheetValidationSeverity.Warning,
                    sheet.Name,
                    sheet.ID,
                    -1,
                    $"Table '{sheet.ID}' was not found in the current database; this sheet will be skipped."));
            }

            if (sheet.Values == null || sheet.Values.Count == 0)
            {
                report.Add(new SheetValidationIssue(
                    SheetValidationSeverity.Error,
                    sheet.Name,
                    sheet.ID,
                    0,
                    $"Sheet is missing the {SheetConfig.ROW_NAME_MARKER} header row."));
                return;
            }

            var nameRow = sheet.Values[0];
            if (nameRow == null || nameRow.Count == 0 || nameRow[0] != SheetConfig.ROW_NAME_MARKER)
            {
                report.Add(new SheetValidationIssue(
                    SheetValidationSeverity.Error,
                    sheet.Name,
                    sheet.ID,
                    0,
                    $"First row must start with {SheetConfig.ROW_NAME_MARKER}."));
                return;
            }

            var dataStartIndex = 1;
            if (sheet.Values.Count > 1
                && sheet.Values[1] != null
                && sheet.Values[1].Count > 0
                && sheet.Values[1][0] == SheetConfig.ROW_TYPE_MARKER)
            {
                dataStartIndex = 2;
            }
            else
            {
                report.Add(new SheetValidationIssue(
                    SheetValidationSeverity.Warning,
                    sheet.Name,
                    sheet.ID,
                    1,
                    $"Sheet is missing the {SheetConfig.ROW_TYPE_MARKER} row; import will continue using existing field types."));
            }

            ValidateRowIds(sheet, dataStartIndex, report);
        }

        private static void ValidateRowIds(
            SheetInfo sheet,
            int dataStartIndex,
            SheetValidationReport report)
        {
            var seen = new HashSet<string>();
            for (var rowIndex = dataStartIndex; rowIndex < sheet.Values.Count; rowIndex++)
            {
                var row = sheet.Values[rowIndex];
                if (row == null || row.Count == 0)
                    continue;

                var firstCell = row[0];
                if (!string.IsNullOrEmpty(firstCell) && firstCell.StartsWith(SheetConfig.ROW_COMMENT_PREFIX))
                    continue;

                if (row.Count <= 1 || string.IsNullOrEmpty(row[1]))
                {
                    report.Add(new SheetValidationIssue(
                        SheetValidationSeverity.Error,
                        sheet.Name,
                        sheet.ID,
                        rowIndex,
                        "Data row has an empty row ID."));
                    continue;
                }

                var rowId = row[1];
                if (!seen.Add(rowId))
                {
                    report.Add(new SheetValidationIssue(
                        SheetValidationSeverity.Error,
                        sheet.Name,
                        sheet.ID,
                        rowIndex,
                        $"Duplicate row ID '{rowId}' in this sheet."));
                }
            }
        }
    }
}
