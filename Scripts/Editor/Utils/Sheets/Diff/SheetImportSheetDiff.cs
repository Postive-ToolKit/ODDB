using System.Collections.Generic;
using System.Linq;

namespace TeamODD.ODDB.Editors.Utils.Sheets.Diff
{
    public sealed class SheetImportSheetDiff
    {
        private readonly List<SheetImportRowDiff> _rows = new();

        public string SheetName { get; }
        public string SheetId { get; }
        public bool Skipped { get; }
        public string SkipReason { get; }
        public IReadOnlyList<SheetImportRowDiff> Rows => _rows;
        public int AddedRows => Count(SheetImportDiffKind.Added);
        public int UpdatedRows => Count(SheetImportDiffKind.Updated);
        public int RemovedRows => Count(SheetImportDiffKind.Removed);
        public int UnchangedRows => Count(SheetImportDiffKind.Unchanged);

        public SheetImportSheetDiff(string sheetName, string sheetId, bool skipped = false, string skipReason = "")
        {
            SheetName = sheetName ?? string.Empty;
            SheetId = sheetId ?? string.Empty;
            Skipped = skipped;
            SkipReason = skipReason ?? string.Empty;
        }

        public void AddRow(SheetImportRowDiff row)
        {
            if (row != null)
                _rows.Add(row);
        }

        private int Count(SheetImportDiffKind kind)
        {
            return _rows.Count(row => row.Kind == kind);
        }
    }
}
