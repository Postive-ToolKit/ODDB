using System.Collections.Generic;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal enum GoogleSheetColumnOperationKind
    {
        Insert,
        Append,
        Move,
        Delete
    }

    internal sealed class GoogleSheetColumnOperation
    {
        public GoogleSheetColumnOperationKind Kind { get; set; }
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }
        public string ColumnKey { get; set; }
        public string DisplayName { get; set; }
    }

    internal sealed class GoogleSheetColumnMetadataWrite
    {
        public int ColumnIndex { get; set; }
        public string ColumnKey { get; set; }
    }

    internal sealed class GoogleSheetColumnSyncPlan
    {
        public List<int> ManagedColumnIndices { get; } = new List<int>();
        public List<GoogleSheetColumnOperation> Operations { get; } = new List<GoogleSheetColumnOperation>();
        public List<GoogleSheetColumnMetadataWrite> MetadataWrites { get; } = new List<GoogleSheetColumnMetadataWrite>();
        public List<string> DeletedColumnNames { get; } = new List<string>();
    }
}
