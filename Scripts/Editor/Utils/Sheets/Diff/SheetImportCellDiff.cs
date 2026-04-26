namespace TeamODD.ODDB.Editors.Utils.Sheets.Diff
{
    public sealed class SheetImportCellDiff
    {
        public string ColumnName { get; }
        public string OldValue { get; }
        public string NewValue { get; }

        public SheetImportCellDiff(string columnName, string oldValue, string newValue)
        {
            ColumnName = columnName ?? string.Empty;
            OldValue = oldValue ?? string.Empty;
            NewValue = newValue ?? string.Empty;
        }

        public override string ToString()
        {
            return $"{ColumnName}: {OldValue} -> {NewValue}";
        }
    }
}
