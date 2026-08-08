using System.Collections.Generic;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal sealed class GoogleSheetSnapshot
    {
        public int SheetId { get; set; }
        public string Title { get; set; }
        public int ColumnCount { get; set; }
        public int RowCount { get; set; }
        public string TableId { get; set; }
        public bool HasTableMetadata { get; set; }
        public bool HasSchemaVersionMetadata { get; set; }
        public List<List<string>> Values { get; set; } = new List<List<string>>();
        public Dictionary<int, string> ColumnKeys { get; } = new Dictionary<int, string>();
        public Dictionary<SheetCellAddress, string> CellNotes { get; } =
            new Dictionary<SheetCellAddress, string>();
    }
}
