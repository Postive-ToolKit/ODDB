using System;

namespace TeamODD.ODDB.Editors.Utils.Sheets
{
    public readonly struct ExportScope : IEquatable<ExportScope>
    {
        public bool All { get; }
        public string TargetTableId { get; }
        public string TargetViewId { get; }
        public bool IsSingleTable => !All && !string.IsNullOrEmpty(TargetTableId);
        public bool IsViewSubtree => !All && !string.IsNullOrEmpty(TargetViewId);

        private ExportScope(bool all, string tableId, string viewId)
        {
            All = all;
            TargetTableId = tableId;
            TargetViewId = viewId;
        }

        public static ExportScope EntireDatabase => new ExportScope(true, null, null);

        public static ExportScope SingleTable(string tableId)
        {
            if (string.IsNullOrEmpty(tableId))
                throw new ArgumentException("tableId required for SingleTable scope", nameof(tableId));
            return new ExportScope(false, tableId, null);
        }

        public static ExportScope ViewSubtree(string viewId)
        {
            if (string.IsNullOrEmpty(viewId))
                throw new ArgumentException("viewId required for ViewSubtree scope", nameof(viewId));
            return new ExportScope(false, null, viewId);
        }

        public bool Equals(ExportScope other)
        {
            return All == other.All
                   && string.Equals(TargetTableId, other.TargetTableId)
                   && string.Equals(TargetViewId, other.TargetViewId);
        }

        public override bool Equals(object obj) => obj is ExportScope other && Equals(other);

        public override int GetHashCode()
        {
            var h = All ? 1 : 0;
            h = (h * 397) ^ (TargetTableId?.GetHashCode() ?? 0);
            return (h * 397) ^ (TargetViewId?.GetHashCode() ?? 0);
        }

        public override string ToString()
        {
            if (All) return "EntireDatabase";
            if (IsSingleTable) return $"SingleTable({TargetTableId})";
            if (IsViewSubtree) return $"ViewSubtree({TargetViewId})";
            return "InvalidScope";
        }

        public static bool operator ==(ExportScope left, ExportScope right) => left.Equals(right);
        public static bool operator !=(ExportScope left, ExportScope right) => !left.Equals(right);
    }
}
