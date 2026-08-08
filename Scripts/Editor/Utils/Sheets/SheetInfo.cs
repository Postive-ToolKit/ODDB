using System.Collections.Generic;

namespace TeamODD.ODDB.Editors.Utils.Sheets
{
    internal readonly struct SheetCellAddress : System.IEquatable<SheetCellAddress>
    {
        public SheetCellAddress(int rowIndex, int columnIndex)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }

        public int RowIndex { get; }
        public int ColumnIndex { get; }

        public bool Equals(SheetCellAddress other)
        {
            return RowIndex == other.RowIndex && ColumnIndex == other.ColumnIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is SheetCellAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (RowIndex * 397) ^ ColumnIndex;
        }
    }

    public class SheetInfo
    {
        public string Name { get; set; }
        public string ID { get; set; }
        /// <summary>
        /// Set when a physical grouped sheet is unpacked. Empty for legacy
        /// per-table sheets. This is transport metadata and is never persisted
        /// into the ODDB database.
        /// </summary>
        public string SourceGroupID { get; set; }
        public List<List<string>> Values { get; set; }
        internal Dictionary<SheetCellAddress, string> CellNotes { get; } =
            new Dictionary<SheetCellAddress, string>();
        
        public SheetInfo()
        {
            Values = new List<List<string>>();
        }
        
        public SheetInfo(string name, string id)
        {
            Name = name;
            ID = id;
            Values = new List<List<string>>();
        }
        
        public SheetInfo(string name, List<List<string>> values)
        {
            Name = name;
            Values = values ?? new List<List<string>>();
        }
        
        /// <summary>
        /// 행 수를 반환
        /// </summary>
        public int RowCount => Values?.Count ?? 0;
        
        /// <summary>
        /// 시트가 비어있는지 확인
        /// </summary>
        public bool IsEmpty => RowCount == 0;
        
        /// <summary>
        /// 시트 정보를 문자열로 표현
        /// </summary>
        public override string ToString()
        {
            return $"SheetInfo: {Name ?? "Unknown"} ({RowCount} rows)";
        }
    }
}
