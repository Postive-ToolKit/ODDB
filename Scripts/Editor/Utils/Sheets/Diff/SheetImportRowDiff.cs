using System.Collections.Generic;

namespace TeamODD.ODDB.Editors.Utils.Sheets.Diff
{
    public sealed class SheetImportRowDiff
    {
        public SheetImportDiffKind Kind { get; }
        public string RowId { get; }
        public string Summary { get; }
        public IReadOnlyList<SheetImportCellDiff> CellChanges { get; }

        public SheetImportRowDiff(
            SheetImportDiffKind kind,
            string rowId,
            string summary,
            IReadOnlyList<SheetImportCellDiff> cellChanges = null)
        {
            Kind = kind;
            RowId = rowId ?? string.Empty;
            Summary = summary ?? string.Empty;
            CellChanges = cellChanges ?? new List<SheetImportCellDiff>();
        }
    }
}
