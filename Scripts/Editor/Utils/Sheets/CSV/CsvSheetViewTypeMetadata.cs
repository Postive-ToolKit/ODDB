using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.Utils.Sheets.CSV
{
    internal static class CsvSheetViewTypeMetadata
    {
        public static SheetInfo PrepareForExport(SheetInfo source, ODDatabase database)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = Clone(source);
            for (var rowIndex = 0; rowIndex < result.Values.Count; rowIndex++)
            {
                var typeRow = result.Values[rowIndex];
                if (!IsMarker(typeRow, SheetConfig.ROW_TYPE_MARKER))
                    continue;

                if (rowIndex + 1 < result.Values.Count
                    && IsMarker(result.Values[rowIndex + 1], SheetConfig.ROW_VIEW_ID_MARKER))
                {
                    result.Values.RemoveAt(rowIndex + 1);
                }

                var metadataRow = Enumerable.Repeat(string.Empty, typeRow.Count).ToList();
                metadataRow[0] = SheetConfig.ROW_VIEW_ID_MARKER;
                var hasViewReference = false;
                for (var columnIndex = 2; columnIndex < typeRow.Count; columnIndex++)
                {
                    if (!SheetViewTypeReference.TryParse(typeRow[columnIndex], out var viewKey))
                        continue;

                    var view = SheetViewTypeResolver.FindView(database, viewKey);
                    var viewId = view?.ID.ToString() ?? viewKey;
                    var viewName = string.IsNullOrWhiteSpace(view?.Name) ? viewId : view.Name;
                    typeRow[columnIndex] = SheetViewTypeReference.ToDisplayValue(viewName);
                    metadataRow[columnIndex] = viewId;
                    hasViewReference = true;
                }

                if (hasViewReference)
                {
                    result.Values.Insert(rowIndex + 1, metadataRow);
                    rowIndex++;
                }
            }

            return result;
        }

        public static void RestoreForImport(SheetInfo sheet, ODDatabase database)
        {
            if (sheet?.Values == null)
                return;

            for (var rowIndex = 0; rowIndex < sheet.Values.Count; rowIndex++)
            {
                var typeRow = sheet.Values[rowIndex];
                if (!IsMarker(typeRow, SheetConfig.ROW_TYPE_MARKER))
                    continue;

                var metadataIndex = rowIndex + 1;
                if (metadataIndex >= sheet.Values.Count
                    || !IsMarker(sheet.Values[metadataIndex], SheetConfig.ROW_VIEW_ID_MARKER))
                {
                    continue;
                }

                var metadataRow = sheet.Values[metadataIndex];
                for (var columnIndex = 2; columnIndex < typeRow.Count; columnIndex++)
                {
                    if (!SheetViewTypeReference.TryParse(typeRow[columnIndex], out var displayedKey))
                        continue;

                    var stableId = columnIndex < metadataRow.Count
                        ? metadataRow[columnIndex]
                        : string.Empty;
                    var resolvedId = SheetViewTypeResolver.ResolveImportId(
                        database,
                        displayedKey,
                        stableId);
                    typeRow[columnIndex] = SheetViewTypeReference.ToLegacyValue(resolvedId);
                }

                sheet.Values.RemoveAt(metadataIndex);
            }
        }

        private static bool IsMarker(IReadOnlyList<string> row, string marker)
        {
            return row != null
                   && row.Count > 0
                   && string.Equals(row[0], marker, StringComparison.Ordinal);
        }

        private static SheetInfo Clone(SheetInfo source)
        {
            var result = new SheetInfo(source.Name, source.ID)
            {
                SourceGroupID = source.SourceGroupID,
                Values = (source.Values ?? new List<List<string>>())
                    .Select(row => row == null ? new List<string>() : new List<string>(row))
                    .ToList()
            };
            foreach (var pair in source.CellNotes)
                result.CellNotes[pair.Key] = pair.Value;
            return result;
        }
    }
}
