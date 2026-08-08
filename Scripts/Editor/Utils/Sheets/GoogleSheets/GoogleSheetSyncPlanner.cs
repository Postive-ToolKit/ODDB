using System;
using System.Collections.Generic;

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
            var result = new GoogleSheetColumnSyncPlan { ManagedColumnCount = desired.Count };

            for (var targetIndex = 0; targetIndex < desired.Count; targetIndex++)
            {
                var desiredSlot = desired[targetIndex];
                var existingIndex = FindByKey(current, desiredSlot.Key, targetIndex);
                if (existingIndex < 0)
                {
                    var inserted = new ColumnSlot
                    {
                        Key = desiredSlot.Key,
                        DisplayName = desiredSlot.DisplayName,
                        Managed = true,
                        HasMetadata = false
                    };
                    if (targetIndex < current.Count && !current[targetIndex].Managed && current[targetIndex].Reusable)
                    {
                        current[targetIndex] = inserted;
                    }
                    else
                    {
                        current.Insert(targetIndex, inserted);
                        result.Operations.Add(new GoogleSheetColumnOperation
                        {
                            Kind = GoogleSheetColumnOperationKind.Insert,
                            ToIndex = targetIndex,
                            ColumnKey = inserted.Key,
                            DisplayName = inserted.DisplayName
                        });
                    }
                    continue;
                }

                if (existingIndex != targetIndex)
                {
                    var moved = current[existingIndex];
                    current.RemoveAt(existingIndex);
                    current.Insert(targetIndex, moved);
                    result.Operations.Add(new GoogleSheetColumnOperation
                    {
                        Kind = GoogleSheetColumnOperationKind.Move,
                        FromIndex = existingIndex,
                        ToIndex = targetIndex,
                        ColumnKey = moved.Key,
                        DisplayName = moved.DisplayName
                    });
                }
            }

            for (var index = current.Count - 1; index >= desired.Count; index--)
            {
                var slot = current[index];
                if (!slot.Managed)
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

            for (var index = 0; index < desired.Count; index++)
            {
                var slot = current[index];
                if (slot.HasMetadata)
                    continue;

                result.MetadataWrites.Add(new GoogleSheetColumnMetadataWrite
                {
                    ColumnIndex = index,
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
            var currentManagedCount = currentIsGrouped
                ? GroupedSheetCodec.GetManagedColumnCount(new SheetInfo(currentSheet.Title, currentSheet.TableId)
                {
                    Values = currentSheet.Values
                })
                : 0;

            var result = new GoogleSheetColumnSyncPlan { ManagedColumnCount = desiredCount };
            if (!currentIsGrouped)
            {
                for (var index = Math.Max(0, currentSheet.ColumnCount); index < desiredCount; index++)
                {
                    result.Operations.Add(new GoogleSheetColumnOperation
                    {
                        Kind = GoogleSheetColumnOperationKind.Insert,
                        ToIndex = index,
                        ColumnKey = "grouped:" + index,
                        DisplayName = "Grouped column " + (index + 1)
                    });
                }
                return result;
            }

            if (desiredCount > currentManagedCount)
            {
                for (var index = currentManagedCount; index < desiredCount; index++)
                {
                    result.Operations.Add(new GoogleSheetColumnOperation
                    {
                        Kind = GoogleSheetColumnOperationKind.Insert,
                        ToIndex = index,
                        ColumnKey = "grouped:" + index,
                        DisplayName = "Grouped column " + (index + 1)
                    });
                }
            }
            else
            {
                for (var index = currentManagedCount - 1; index >= desiredCount; index--)
                {
                    var displayName = "Grouped column " + (index + 1);
                    result.Operations.Add(new GoogleSheetColumnOperation
                    {
                        Kind = GoogleSheetColumnOperationKind.Delete,
                        FromIndex = index,
                        ColumnKey = "grouped:" + index,
                        DisplayName = displayName
                    });
                    result.DeletedColumnNames.Add(displayName);
                }
            }

            return result;
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
