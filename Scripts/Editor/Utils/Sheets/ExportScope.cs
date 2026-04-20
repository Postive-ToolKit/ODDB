using System;

namespace TeamODD.ODDB.Editors.Utils.Sheets
{
    public readonly struct ExportScope : IEquatable<ExportScope>
    {
        public bool All { get; }
        public string TargetTableId { get; }

        private ExportScope(bool all, string tableId)
        {
            All = all;
            TargetTableId = tableId;
        }

        public static ExportScope EntireDatabase => new ExportScope(true, null);

        public static ExportScope SingleTable(string tableId)
        {
            if (string.IsNullOrEmpty(tableId))
                throw new ArgumentException("tableId required for SingleTable scope", nameof(tableId));
            return new ExportScope(false, tableId);
        }

        public bool Equals(ExportScope other)
        {
            return All == other.All && string.Equals(TargetTableId, other.TargetTableId);
        }

        public override bool Equals(object obj) => obj is ExportScope other && Equals(other);

        public override int GetHashCode()
        {
            var h = All ? 1 : 0;
            return (h * 397) ^ (TargetTableId?.GetHashCode() ?? 0);
        }

        public override string ToString()
        {
            return All ? "EntireDatabase" : $"SingleTable({TargetTableId})";
        }

        public static bool operator ==(ExportScope left, ExportScope right) => left.Equals(right);
        public static bool operator !=(ExportScope left, ExportScope right) => !left.Equals(right);
    }
}
