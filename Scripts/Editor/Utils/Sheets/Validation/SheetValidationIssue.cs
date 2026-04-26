namespace TeamODD.ODDB.Editors.Utils.Sheets.Validation
{
    public sealed class SheetValidationIssue
    {
        public SheetValidationSeverity Severity { get; }
        public string SheetName { get; }
        public string SheetId { get; }
        public int RowIndex { get; }
        public string Message { get; }

        public SheetValidationIssue(
            SheetValidationSeverity severity,
            string sheetName,
            string sheetId,
            int rowIndex,
            string message)
        {
            Severity = severity;
            SheetName = sheetName ?? string.Empty;
            SheetId = sheetId ?? string.Empty;
            RowIndex = rowIndex;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            var row = RowIndex >= 0 ? $", row={RowIndex}" : string.Empty;
            return $"{Severity}: sheet='{SheetName}' id='{SheetId}'{row}: {Message}";
        }
    }
}
