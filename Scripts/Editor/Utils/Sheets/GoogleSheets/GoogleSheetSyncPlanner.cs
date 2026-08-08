using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal static class GoogleSheetSyncPlanner
    {
        private sealed class ColumnSlot
        {
            public string Key;
            public string DisplayName;
            public bool Managed;
            public bool HasMetadata;
            public bool Reusable;
        }

        public static GoogleSheetColumnSyncPlan BuildColumnPlan(
            SheetInfo desiredSheet,
            GoogleSheetSnapshot currentSheet)
        {
            if (desiredSheet == null) throw new ArgumentNullException(nameof(desiredSheet));
            if (currentSheet == null) throw new ArgumentNullException(nameof(currentSheet));

            if (GroupedSheetCodec.IsGrouped(desiredSheet))
                return BuildGroupedColumnPlan(desiredSheet, currentSheet);

            var desired = ReadDesiredColumns(desiredSheet);
            var current = ReadCurrentColumns(currentSheet);
            var result = new GoogleSheetColumnSyncPlan();

            var desiredKeys = new HashSet<string>();
            foreach (var slot in desired)
                desiredKeys.Add(slot.Key);

            // Remove only ODDB-owned columns that no longer exist. User columns are
            // physical anchors and are never moved or deleted by this pass.
            for (var index = current.Count - 1; index >= 0; index--)
            {
                var slot = current[index];
                if (!slot.Managed || desiredKeys.Contains(slot.Key))
                    continue;

                result.Operations.Add(new GoogleSheetColumnOperation
                {
                    Kind = GoogleSheetColumnOperationKind.Delete,
                    FromIndex = index,
                    ColumnKey = slot.Key,
                    DisplayName = slot.DisplayName
                });
                result.DeletedColumnNames.Add(slot.DisplayName);
                current.RemoveAt(index);
            }

            for (var logicalIndex = 0; logicalIndex < desired.Count; logicalIndex++)
            {
                var desiredSlot = desired[logicalIndex];
                var existingIndex = FindByKey(current, desiredSlot.Key, 0);

                // The two system columns retain their fixed A/B positions because row
                // identity lookup depends on them. Field columns after B may be sparse.
                if (logicalIndex < 2)
                {
                    if (existingIndex < 0)
                    {
                        var inserted = CreateManagedSlot(desiredSlot);
                        current.Insert(logicalIndex, inserted);
                        result.Operations.Add(InsertOperation(logicalIndex, inserted));
                        existingIndex = logicalIndex;
                    }
                    else if (existingIndex != logicalIndex)
                    {
                        var moved = current[existingIndex];
                        current.RemoveAt(existingIndex);
                        current.Insert(logicalIndex, moved);
                        result.Operations.Add(new GoogleSheetColumnOperation
                        {
                            Kind = GoogleSheetColumnOperationKind.Move,
                            FromIndex = existingIndex,
                            ToIndex = logicalIndex,
                            ColumnKey = moved.Key,
                            DisplayName = moved.DisplayName
                        });
                        existingIndex = logicalIndex;
                    }
                }
                else if (existingIndex < 0)
                {
                    var previousPhysical = result.ManagedColumnIndices[logicalIndex - 1];
                    var nextExisting = FindNextDesiredColumn(
                        current,
                        desired,
                        logicalIndex + 1,
                        previousPhysical + 1);
                    var searchEnd = nextExisting >= 0 ? nextExisting : current.Count;
                    existingIndex = FindReusable(current, previousPhysical + 1, searchEnd);
                    var inserted = CreateManagedSlot(desiredSlot);
                    if (existingIndex >= 0)
                    {
                        current[existingIndex] = inserted;
                    }
                    else if (nextExisting >= 0)
                    {
                        existingIndex = nextExisting;
                        current.Insert(existingIndex, inserted);
                        result.Operations.Add(InsertOperation(existingIndex, inserted));
                    }
                    else
                    {
                        existingIndex = current.Count;
                        current.Add(inserted);
                        result.Operations.Add(new GoogleSheetColumnOperation
                        {
                            Kind = GoogleSheetColumnOperationKind.Append,
                            ToIndex = existingIndex,
                            ColumnKey = inserted.Key,
                            DisplayName = inserted.DisplayName
                        });
                    }
                }

                result.ManagedColumnIndices.Add(existingIndex);
            }

            for (var index = 0; index < desired.Count; index++)
            {
                var physicalIndex = result.ManagedColumnIndices[index];
                var slot = current[physicalIndex];
                if (slot.HasMetadata)
                    continue;

                result.MetadataWrites.Add(new GoogleSheetColumnMetadataWrite
                {
                    ColumnIndex = physicalIndex,
                    ColumnKey = desired[index].Key
                });
            }

            return result;
        }

        private static GoogleSheetColumnSyncPlan BuildGroupedColumnPlan(
            SheetInfo desiredSheet,
            GoogleSheetSnapshot currentSheet)
        {
            var desiredCount = GroupedSheetCodec.GetManagedColumnCount(desiredSheet);
            var currentIsGrouped = currentSheet.Values != null
                                   && currentSheet.Values.Count > 0
                                   && string.Equals(
                                       GetCell(currentSheet.Values[0], 0),
                                       SheetConfig.GROUP_MARKER,
                                       StringComparison.Ordinal);
            var result = new GoogleSheetColumnSyncPlan();
            var physicalCount = Math.Max(
                Math.Max(1, currentSheet.ColumnCount),
                currentSheet.Values == null
                    ? 0
                    : currentSheet.Values.Where(row => row != null).Select(row => row.Count).DefaultIfEmpty(0).Max());
            var protectedColumns = currentIsGrouped
                ? ReadGroupedProtectedColumns(currentSheet.Values)
                : new HashSet<int>();
            var protectedFlags = Enumerable.Range(0, physicalCount)
                .Select(protectedColumns.Contains)
                .ToList();

            if (currentIsGrouped)
            {
                var compactCurrent = new SheetInfo(currentSheet.Title, currentSheet.TableId)
                {
                    Values = CompactGroupedValues(currentSheet.Values)
                };
                var currentManagedCount = GroupedSheetCodec.GetManagedColumnCount(compactCurrent);
                var currentManagedPhysicalIndices = Enumerable.Range(0, physicalCount)
                    .Where(index => !protectedFlags[index])
                    .Take(currentManagedCount)
                    .ToList();
                for (var logicalIndex = currentManagedPhysicalIndices.Count - 1;
                     logicalIndex >= desiredCount;
                     logicalIndex--)
                {
                    var physicalIndex = currentManagedPhysicalIndices[logicalIndex];
                    var displayName = "Grouped column " + (logicalIndex + 1);
                    result.Operations.Add(new GoogleSheetColumnOperation
                    {
                        Kind = GoogleSheetColumnOperationKind.Delete,
                        FromIndex = physicalIndex,
                        ColumnKey = "grouped:" + logicalIndex,
                        DisplayName = displayName
                    });
                    result.DeletedColumnNames.Add(displayName);
                    protectedFlags.RemoveAt(physicalIndex);
                    physicalCount--;
                }
            }

            for (var physicalIndex = 0;
                 physicalIndex < physicalCount && result.ManagedColumnIndices.Count < desiredCount;
                 physicalIndex++)
            {
                if (!protectedFlags[physicalIndex])
                    result.ManagedColumnIndices.Add(physicalIndex);
            }

            while (result.ManagedColumnIndices.Count < desiredCount)
            {
                var logicalIndex = result.ManagedColumnIndices.Count;
                result.ManagedColumnIndices.Add(physicalCount);
                result.Operations.Add(new GoogleSheetColumnOperation
                {
                    Kind = GoogleSheetColumnOperationKind.Append,
                    ToIndex = physicalCount,
                    ColumnKey = "grouped:" + logicalIndex,
                    DisplayName = "Grouped column " + (logicalIndex + 1)
                });
                physicalCount++;
            }

            return result;
        }

        internal static List<List<string>> CompactGroupedValues(IReadOnlyList<List<string>> values)
        {
            if (values == null)
                return new List<List<string>>();

            var protectedColumns = ReadGroupedProtectedColumns(values);
            var result = new List<List<string>>(values.Count);
            foreach (var row in values)
            {
                var compact = new List<string>();
                if (row != null)
                {
                    for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
                    {
                        if (!protectedColumns.Contains(columnIndex))
                            compact.Add(row[columnIndex] ?? string.Empty);
                    }
                }
                result.Add(compact);
            }
            return result;
        }

        private static HashSet<int> ReadGroupedProtectedColumns(IReadOnlyList<List<string>> values)
        {
            var result = new HashSet<int>();
            if (values == null)
                return result;
            var managedBoundary = 0;

            for (var rowIndex = 0; rowIndex < values.Count; rowIndex++)
            {
                var nameRow = values[rowIndex];
                var marker = GetCell(nameRow, 0);
                if (IsGroupedStructuralMarker(marker))
                    managedBoundary = Math.Max(managedBoundary, LastNonEmptyColumn(nameRow) + 1);

                if (!string.Equals(marker, SheetConfig.ROW_NAME_MARKER, StringComparison.Ordinal))
                    continue;

                for (var columnIndex = 2; columnIndex < nameRow.Count; columnIndex++)
                {
                    var name = GetCell(nameRow, columnIndex);
                    if (!string.IsNullOrEmpty(name)
                        && name.StartsWith(SheetConfig.IGNORE_PREFIX, StringComparison.Ordinal))
                    {
                        result.Add(columnIndex);
                    }
                }
            }

            var maxWidth = values.Where(row => row != null).Select(row => row.Count).DefaultIfEmpty(0).Max();
            for (var columnIndex = managedBoundary; columnIndex < maxWidth; columnIndex++)
            {
                if (values.Any(row => !string.IsNullOrEmpty(GetCell(row, columnIndex))))
                    result.Add(columnIndex);
            }

            return result;
        }

        private static bool IsGroupedStructuralMarker(string marker)
        {
            return string.Equals(marker, SheetConfig.GROUP_MARKER, StringComparison.Ordinal)
                   || string.Equals(marker, SheetConfig.TABLE_MARKER, StringComparison.Ordinal)
                   || string.Equals(marker, SheetConfig.ROW_NAME_MARKER, StringComparison.Ordinal)
                   || string.Equals(marker, SheetConfig.ROW_TYPE_MARKER, StringComparison.Ordinal)
                   || string.Equals(marker, SheetConfig.TABLE_END_MARKER, StringComparison.Ordinal)
                   || string.Equals(marker, SheetConfig.GROUP_END_MARKER, StringComparison.Ordinal);
        }

        private static int LastNonEmptyColumn(IReadOnlyList<string> row)
        {
            if (row == null)
                return -1;
            for (var index = row.Count - 1; index >= 0; index--)
            {
                if (!string.IsNullOrEmpty(GetCell(row, index)))
                    return index;
            }
            return -1;
        }

        private static ColumnSlot CreateManagedSlot(ColumnSlot desired)
        {
            return new ColumnSlot
            {
                Key = desired.Key,
                DisplayName = desired.DisplayName,
                Managed = true,
                HasMetadata = false
            };
        }

        private static GoogleSheetColumnOperation InsertOperation(int index, ColumnSlot slot)
        {
            return new GoogleSheetColumnOperation
            {
                Kind = GoogleSheetColumnOperationKind.Insert,
                ToIndex = index,
                ColumnKey = slot.Key,
                DisplayName = slot.DisplayName
            };
        }

        private static int FindReusable(IReadOnlyList<ColumnSlot> columns, int startIndex, int endIndex)
        {
            for (var index = Math.Max(0, startIndex); index < Math.Min(endIndex, columns.Count); index++)
            {
                if (!columns[index].Managed && columns[index].Reusable)
                    return index;
            }
            return -1;
        }

        private static int FindNextDesiredColumn(
            IReadOnlyList<ColumnSlot> current,
            IReadOnlyList<ColumnSlot> desired,
            int desiredStartIndex,
            int physicalStartIndex)
        {
            var best = -1;
            for (var index = desiredStartIndex; index < desired.Count; index++)
            {
                var physicalIndex = FindByKey(current, desired[index].Key, physicalStartIndex);
                if (physicalIndex >= 0 && (best < 0 || physicalIndex < best))
                    best = physicalIndex;
            }
            return best;
        }

        private static List<ColumnSlot> ReadDesiredColumns(SheetInfo sheet)
        {
            if (sheet.Values == null || sheet.Values.Count == 0 || sheet.Values[0] == null)
                throw new InvalidOperationException($"Sheet '{sheet.Name}' is missing the {SheetConfig.ROW_NAME_MARKER} header row.");

            var header = sheet.Values[0];
            var result = new List<ColumnSlot>
            {
                Managed(GoogleSheetConfig.SYSTEM_NAME_COLUMN_KEY, SheetConfig.ROW_NAME_MARKER, true),
                Managed(GoogleSheetConfig.SYSTEM_ID_COLUMN_KEY, "ID", true)
            };
            var fieldNames = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 2; index < header.Count; index++)
            {
                var fieldName = header[index]?.Trim();
                if (string.IsNullOrEmpty(fieldName))
                    throw new InvalidOperationException($"Sheet '{sheet.Name}' has an empty ODDB field name at column {index + 1}.");
                if (!fieldNames.Add(fieldName))
                    throw new InvalidOperationException($"Sheet '{sheet.Name}' has duplicate ODDB field name '{fieldName}'.");

                result.Add(Managed(GoogleSheetConfig.FIELD_COLUMN_PREFIX + fieldName, fieldName, true));
            }
            return result;
        }

        private static List<ColumnSlot> ReadCurrentColumns(GoogleSheetSnapshot sheet)
        {
            var header = GetRow(sheet.Values, 0);
            var types = GetRow(sheet.Values, 1);
            var count = Math.Max(Math.Max(sheet.ColumnCount, header.Count), Math.Max(types.Count, 2));
            var result = new List<ColumnSlot>(count);

            for (var index = 0; index < count; index++)
            {
                var displayName = GetCell(header, index);
                if (sheet.ColumnKeys.TryGetValue(index, out var metadataKey)
                    && !string.IsNullOrEmpty(metadataKey))
                {
                    result.Add(Managed(metadataKey, displayName, true));
                    continue;
                }

                if (index == 0)
                {
                    result.Add(Managed(GoogleSheetConfig.SYSTEM_NAME_COLUMN_KEY, SheetConfig.ROW_NAME_MARKER, false));
                    continue;
                }
                if (index == 1)
                {
                    result.Add(Managed(GoogleSheetConfig.SYSTEM_ID_COLUMN_KEY, "ID", false));
                    continue;
                }

                var type = GetCell(types, index);
                if (displayName.StartsWith("#REMOVED ", StringComparison.Ordinal))
                {
                    result.Add(Managed("legacy-removed:" + index, displayName.Substring(9), false));
                }
                else if (!string.IsNullOrEmpty(displayName)
                         && !displayName.StartsWith(SheetConfig.IGNORE_PREFIX, StringComparison.Ordinal)
                         && !string.IsNullOrEmpty(type))
                {
                    result.Add(Managed(GoogleSheetConfig.FIELD_COLUMN_PREFIX + displayName, displayName, false));
                }
                else
                {
                    result.Add(new ColumnSlot
                    {
                        DisplayName = displayName,
                        Managed = false,
                        Reusable = IsColumnEmpty(sheet.Values, index)
                    });
                }
            }

            return result;
        }

        private static int FindByKey(IReadOnlyList<ColumnSlot> columns, string key, int startIndex)
        {
            for (var index = startIndex; index < columns.Count; index++)
            {
                if (columns[index].Managed && string.Equals(columns[index].Key, key, StringComparison.Ordinal))
                    return index;
            }
            return -1;
        }

        private static ColumnSlot Managed(string key, string displayName, bool hasMetadata)
        {
            return new ColumnSlot
            {
                Key = key,
                DisplayName = string.IsNullOrEmpty(displayName) ? key : displayName,
                Managed = true,
                HasMetadata = hasMetadata
            };
        }

        private static List<string> GetRow(IReadOnlyList<List<string>> values, int index)
        {
            return values != null && index >= 0 && index < values.Count && values[index] != null
                ? values[index]
                : new List<string>();
        }

        private static string GetCell(IReadOnlyList<string> row, int index)
        {
            return row != null && index >= 0 && index < row.Count
                ? row[index] ?? string.Empty
                : string.Empty;
        }

        private static bool IsColumnEmpty(IReadOnlyList<List<string>> values, int columnIndex)
        {
            if (values == null)
                return true;
            for (var rowIndex = 0; rowIndex < values.Count; rowIndex++)
            {
                if (!string.IsNullOrEmpty(GetCell(values[rowIndex], columnIndex)))
                    return false;
            }
            return true;
        }
    }
}
