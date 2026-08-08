using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.Utils.Sheets
{
    internal static class GroupedSheetCodec
    {
        public static bool IsGrouped(SheetInfo sheet)
        {
            return sheet?.Values != null
                   && sheet.Values.Count > 0
                   && string.Equals(GetCell(sheet.Values[0], 0), SheetConfig.GROUP_MARKER, StringComparison.Ordinal);
        }

        internal static string GetGroupId(SheetInfo sheet)
        {
            return IsGrouped(sheet) ? GetCell(sheet.Values[0], 1) : string.Empty;
        }

        public static SheetInfo Pack(string groupName, string groupId, IReadOnlyList<SheetInfo> tables)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("A group ID is required.", nameof(groupId));
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));

            var result = new SheetInfo(groupName ?? groupId, groupId)
            {
                SourceGroupID = groupId
            };
            result.Values.Add(new List<string>
            {
                SheetConfig.GROUP_MARKER,
                groupId,
                groupName ?? groupId,
                SheetConfig.GROUP_LAYOUT_VERSION
            });

            var tableIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var table in tables)
            {
                if (table == null || string.IsNullOrWhiteSpace(table.ID))
                    throw new InvalidOperationException($"Grouped sheet '{groupName}' contains a table without an ID.");
                if (!tableIds.Add(table.ID))
                    throw new InvalidOperationException($"Grouped sheet '{groupName}' contains duplicate table ID '{table.ID}'.");

                result.Values.Add(new List<string>
                {
                    SheetConfig.TABLE_MARKER,
                    table.ID,
                    table.Name ?? table.ID
                });
                foreach (var row in table.Values ?? new List<List<string>>())
                    result.Values.Add(row == null ? new List<string>() : new List<string>(row));
                result.Values.Add(new List<string> { SheetConfig.TABLE_END_MARKER, table.ID });
            }

            result.Values.Add(new List<string> { SheetConfig.GROUP_END_MARKER, groupId });
            return result;
        }

        public static List<SheetInfo> UnpackAll(IEnumerable<SheetInfo> physicalSheets)
        {
            var result = new List<SheetInfo>();
            if (physicalSheets == null)
                return result;

            foreach (var sheet in physicalSheets)
            {
                if (sheet == null)
                    continue;
                if (IsGrouped(sheet))
                    result.AddRange(Unpack(sheet));
                else
                    result.Add(sheet);
            }
            return result;
        }

        public static List<SheetInfo> Unpack(SheetInfo groupedSheet)
        {
            if (!IsGrouped(groupedSheet))
                throw new InvalidOperationException($"Sheet '{groupedSheet?.Name}' is not an ODDB grouped sheet.");

            var header = groupedSheet.Values[0];
            var groupId = GetCell(header, 1);
            var version = GetCell(header, 3);
            if (string.IsNullOrWhiteSpace(groupId))
                throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' has no group ID.");
            if (!string.Equals(version, SheetConfig.GROUP_LAYOUT_VERSION, StringComparison.Ordinal))
                throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' uses unsupported layout version '{version}'.");

            var result = new List<SheetInfo>();
            var tableIds = new HashSet<string>(StringComparer.Ordinal);
            SheetInfo current = null;
            var foundGroupEnd = false;

            for (var rowIndex = 1; rowIndex < groupedSheet.Values.Count; rowIndex++)
            {
                var row = groupedSheet.Values[rowIndex] ?? new List<string>();
                var marker = GetCell(row, 0);

                if (string.Equals(marker, SheetConfig.TABLE_MARKER, StringComparison.Ordinal))
                {
                    if (current != null)
                        throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' starts a table before ending table '{current.ID}'.");

                    var tableId = GetCell(row, 1);
                    if (string.IsNullOrWhiteSpace(tableId))
                        throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' has a table marker without an ID at row {rowIndex + 1}.");
                    if (!tableIds.Add(tableId))
                        throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' contains duplicate table ID '{tableId}'.");

                    current = new SheetInfo(GetCell(row, 2), tableId)
                    {
                        SourceGroupID = groupId
                    };
                    if (string.IsNullOrEmpty(current.Name))
                        current.Name = tableId;
                    continue;
                }

                if (string.Equals(marker, SheetConfig.TABLE_END_MARKER, StringComparison.Ordinal))
                {
                    if (current == null)
                        throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' has an unmatched table end marker at row {rowIndex + 1}.");
                    var endId = GetCell(row, 1);
                    if (!string.IsNullOrEmpty(endId) && !string.Equals(endId, current.ID, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' ends table '{endId}' while parsing '{current.ID}'.");
                    ValidateTable(current, groupedSheet.Name);
                    result.Add(current);
                    current = null;
                    continue;
                }

                if (string.Equals(marker, SheetConfig.GROUP_END_MARKER, StringComparison.Ordinal))
                {
                    if (current != null)
                        throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' ends before table '{current.ID}' is closed.");
                    foundGroupEnd = true;
                    break;
                }

                if (current != null)
                {
                    current.Values.Add(new List<string>(row));
                    continue;
                }

                if (!IsIgnorableOutsideTable(row))
                    throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' contains data outside a table block at row {rowIndex + 1}.");
            }

            if (current != null)
                throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' does not end table '{current.ID}'.");
            if (!foundGroupEnd)
                throw new InvalidOperationException($"Grouped sheet '{groupedSheet.Name}' is missing {SheetConfig.GROUP_END_MARKER}.");
            return result;
        }

        public static int GetManagedRowCount(SheetInfo groupedSheet)
        {
            if (!IsGrouped(groupedSheet))
                return groupedSheet?.Values?.Count ?? 0;
            for (var index = 0; index < groupedSheet.Values.Count; index++)
            {
                if (string.Equals(GetCell(groupedSheet.Values[index], 0), SheetConfig.GROUP_END_MARKER, StringComparison.Ordinal))
                    return index + 1;
            }
            return groupedSheet.Values.Count;
        }

        public static int GetManagedColumnCount(SheetInfo groupedSheet)
        {
            if (groupedSheet?.Values == null)
                return 0;

            var max = 0;
            var managedRowCount = GetManagedRowCount(groupedSheet);
            for (var rowIndex = 0; rowIndex < managedRowCount; rowIndex++)
            {
                var row = groupedSheet.Values[rowIndex];
                var marker = GetCell(row, 0);
                if (string.Equals(marker, SheetConfig.GROUP_MARKER, StringComparison.Ordinal)
                    || string.Equals(marker, SheetConfig.TABLE_MARKER, StringComparison.Ordinal)
                    || string.Equals(marker, SheetConfig.TABLE_END_MARKER, StringComparison.Ordinal)
                    || string.Equals(marker, SheetConfig.GROUP_END_MARKER, StringComparison.Ordinal)
                    || string.Equals(marker, SheetConfig.ROW_NAME_MARKER, StringComparison.Ordinal)
                    || string.Equals(marker, SheetConfig.ROW_TYPE_MARKER, StringComparison.Ordinal))
                {
                    max = Math.Max(max, row?.Count ?? 0);
                }
            }
            return Math.Max(2, max);
        }

        private static void ValidateTable(SheetInfo table, string groupedSheetName)
        {
            var nameRowIndex = table.Values.FindIndex(row =>
                string.Equals(GetCell(row, 0), SheetConfig.ROW_NAME_MARKER, StringComparison.Ordinal));
            if (nameRowIndex < 0)
                throw new InvalidOperationException($"Table '{table.ID}' in grouped sheet '{groupedSheetName}' is missing {SheetConfig.ROW_NAME_MARKER}.");
            if (nameRowIndex != 0)
                throw new InvalidOperationException($"Table '{table.ID}' in grouped sheet '{groupedSheetName}' must start with {SheetConfig.ROW_NAME_MARKER}.");
        }

        private static bool IsIgnorableOutsideTable(IReadOnlyList<string> row)
        {
            if (row == null || row.Count == 0 || row.All(string.IsNullOrEmpty))
                return true;
            var marker = GetCell(row, 0);
            return !string.IsNullOrEmpty(marker)
                   && marker.StartsWith(SheetConfig.ROW_COMMENT_PREFIX, StringComparison.Ordinal);
        }

        private static string GetCell(IReadOnlyList<string> row, int index)
        {
            return row != null && index >= 0 && index < row.Count
                ? row[index] ?? string.Empty
                : string.Empty;
        }
    }

    internal static class SheetLayoutPlanner
    {
        public static IReadOnlyList<SheetInfo> Pack(
            IReadOnlyList<SheetInfo> logicalSheets,
            ODDatabase database,
            SheetLayoutMode mode)
        {
            if (logicalSheets == null) throw new ArgumentNullException(nameof(logicalSheets));
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (mode == SheetLayoutMode.PerTable)
                return logicalSheets.Where(sheet => sheet != null).ToList();

            var output = new List<SheetInfo>();
            var groups = new Dictionary<string, GroupBucket>(StringComparer.Ordinal);
            foreach (var sheet in logicalSheets.Where(sheet => sheet != null))
            {
                if (database.Tables.Read(new ODDBID(sheet.ID)) is not Table table)
                {
                    output.Add(sheet);
                    continue;
                }

                var root = FindRootView(table);
                if (root == null)
                {
                    output.Add(sheet);
                    continue;
                }

                var groupId = root.ID.ToString();
                if (!groups.TryGetValue(groupId, out var group))
                {
                    group = new GroupBucket(root.Name, groupId);
                    groups.Add(groupId, group);
                    output.Add(group.Placeholder);
                }
                group.Tables.Add(sheet);
            }

            for (var index = 0; index < output.Count; index++)
            {
                var placeholder = output[index];
                if (placeholder == null || !groups.TryGetValue(placeholder.ID, out var group))
                    continue;
                output[index] = GroupedSheetCodec.Pack(group.Name, group.Id, group.Tables);
            }
            return output;
        }

        public static IReadOnlyList<SheetInfo> SelectLogicalSheetsForExport(
            ODDatabase database,
            ODDBSheetConverter converter,
            ExportScope scope,
            SheetLayoutMode mode)
        {
            if (scope.All)
                return converter.GetAllSheets();

            if (database.Tables.Read(new ODDBID(scope.TargetTableId)) is not Table selected)
                throw new InvalidOperationException($"Table '{scope.TargetTableId}' not found in current database.");

            if (mode == SheetLayoutMode.PerTable || FindRootView(selected) == null)
                return new List<SheetInfo> { converter.ExportTable(selected) };

            var rootId = FindRootView(selected).ID.ToString();
            var result = new List<SheetInfo>();
            foreach (var candidate in database.Tables.GetAll().OfType<Table>())
            {
                var candidateRoot = FindRootView(candidate);
                if (candidateRoot != null && string.Equals(candidateRoot.ID.ToString(), rootId, StringComparison.Ordinal))
                    result.Add(converter.ExportTable(candidate));
            }
            return result;
        }

        public static IReadOnlyList<SheetInfo> FilterImportedSheets(
            IReadOnlyList<SheetInfo> sheets,
            ODDatabase database,
            SheetLayoutMode mode)
        {
            if (sheets == null) throw new ArgumentNullException(nameof(sheets));
            if (database == null) throw new ArgumentNullException(nameof(database));

            var result = new List<SheetInfo>();
            foreach (var candidates in sheets
                         .Where(sheet => sheet != null)
                         .GroupBy(sheet => sheet.ID, StringComparer.Ordinal))
            {
                if (string.IsNullOrEmpty(candidates.Key))
                {
                    result.AddRange(candidates);
                    continue;
                }
                if (database.Tables.Read(new ODDBID(candidates.Key)) is not Table table)
                {
                    result.AddRange(candidates);
                    continue;
                }

                var rootId = FindRootView(table)?.ID.ToString();
                List<SheetInfo> preferred;
                if (mode == SheetLayoutMode.GroupByRootView && !string.IsNullOrEmpty(rootId))
                {
                    preferred = candidates
                        .Where(sheet => string.Equals(sheet.SourceGroupID, rootId, StringComparison.Ordinal))
                        .ToList();
                    if (preferred.Count == 0)
                    {
                        preferred = candidates
                            .Where(sheet => string.IsNullOrEmpty(sheet.SourceGroupID))
                            .ToList();
                    }
                }
                else
                {
                    preferred = candidates
                        .Where(sheet => string.IsNullOrEmpty(sheet.SourceGroupID))
                        .ToList();
                    if (preferred.Count == 0)
                        preferred = candidates.ToList();
                }

                result.AddRange(preferred);
            }
            return result;
        }

        public static IReadOnlyList<SheetInfo> FilterPhysicalSheetsForImport(
            IReadOnlyList<SheetInfo> physicalSheets,
            ODDatabase database,
            ExportScope scope,
            SheetLayoutMode mode)
        {
            if (physicalSheets == null) throw new ArgumentNullException(nameof(physicalSheets));
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (scope.All)
                return physicalSheets.Where(sheet => sheet != null).ToList();

            var rootId = database.Tables.Read(new ODDBID(scope.TargetTableId)) is Table table
                ? FindRootView(table)?.ID.ToString()
                : null;
            var legacy = physicalSheets
                .Where(sheet => sheet != null
                                && !GroupedSheetCodec.IsGrouped(sheet)
                                && string.Equals(sheet.ID, scope.TargetTableId, StringComparison.Ordinal))
                .ToList();
            var grouped = string.IsNullOrEmpty(rootId)
                ? new List<SheetInfo>()
                : physicalSheets
                    .Where(sheet => sheet != null
                                    && GroupedSheetCodec.IsGrouped(sheet)
                                    && string.Equals(
                                        GroupedSheetCodec.GetGroupId(sheet),
                                        rootId,
                                        StringComparison.Ordinal))
                    .ToList();

            if (mode == SheetLayoutMode.GroupByRootView && grouped.Count > 0)
                return grouped;
            if (legacy.Count > 0)
                return legacy;
            return grouped;
        }

        internal static IView FindRootView(Table table)
        {
            if (table?.ParentView == null)
                return null;

            var current = table.ParentView;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (current?.ParentView != null && visited.Add(current.ID.ToString()))
                current = current.ParentView;
            return current;
        }

        private sealed class GroupBucket
        {
            public GroupBucket(string name, string id)
            {
                Name = string.IsNullOrEmpty(name) ? id : name;
                Id = id;
                Placeholder = new SheetInfo(Name, Id);
            }

            public string Name { get; }
            public string Id { get; }
            public SheetInfo Placeholder { get; }
            public List<SheetInfo> Tables { get; } = new List<SheetInfo>();
        }
    }
}
