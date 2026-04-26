using System.Collections.Generic;
using System.Linq;

namespace TeamODD.ODDB.Editors.Utils.Sheets.Diff
{
    public sealed class SheetImportDiffReport
    {
        private readonly List<SheetImportSheetDiff> _sheets = new();

        public IReadOnlyList<SheetImportSheetDiff> Sheets => _sheets;
        public int SkippedSheetCount => _sheets.Count(sheet => sheet.Skipped);
        public int TotalAddedRows => _sheets.Sum(sheet => sheet.AddedRows);
        public int TotalUpdatedRows => _sheets.Sum(sheet => sheet.UpdatedRows);
        public int TotalRemovedRows => _sheets.Sum(sheet => sheet.RemovedRows);
        public int TotalUnchangedRows => _sheets.Sum(sheet => sheet.UnchangedRows);

        public void AddSheet(SheetImportSheetDiff sheet)
        {
            if (sheet != null)
                _sheets.Add(sheet);
        }
    }
}
