using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal static class GoogleSheetViewTypeNotes
    {
        internal const string NotePrefix = "ODDB View ID: ";

        public static IReadOnlyList<SheetInfo> PrepareForExport(
            IReadOnlyList<SheetInfo> sheets,
            ODDatabase database)
        {
            if (sheets == null) throw new ArgumentNullException(nameof(sheets));

            var result = new List<SheetInfo>(sheets.Count);
            foreach (var source in sheets)
            {
                if (source == null)
                    continue;

                var copy = Clone(source);
                for (var rowIndex = 0; rowIndex < copy.Values.Count; rowIndex++)
                {
                    var row = copy.Values[rowIndex];
                    if (!IsTypeRow(row))
                        continue;

                    for (var columnIndex = 2; columnIndex < row.Count; columnIndex++)
                    {
                        if (!SheetViewTypeReference.TryParse(row[columnIndex], out var viewKey))
                            continue;

                        var view = SheetViewTypeResolver.FindView(database, viewKey);
                        var viewId = view?.ID.ToString() ?? viewKey;
                        var viewName = string.IsNullOrWhiteSpace(view?.Name) ? viewId : view.Name;
                        row[columnIndex] = SheetViewTypeReference.ToDisplayValue(viewName);
                        copy.CellNotes[new SheetCellAddress(rowIndex, columnIndex)] = NotePrefix + viewId;
                    }
                }
                result.Add(copy);
            }
            return result;
        }

        public static void RestoreForImport(GoogleSheetSnapshot snapshot, ODDatabase database)
        {
            if (snapshot?.Values == null)
                return;

            for (var rowIndex = 0; rowIndex < snapshot.Values.Count; rowIndex++)
            {
                var row = snapshot.Values[rowIndex];
                if (!IsTypeRow(row))
                    continue;

                for (var columnIndex = 2; columnIndex < row.Count; columnIndex++)
                {
                    if (!SheetViewTypeReference.TryParse(row[columnIndex], out var displayedKey))
                        continue;

                    var address = new SheetCellAddress(rowIndex, columnIndex);
                    snapshot.CellNotes.TryGetValue(address, out var note);
                    var notedId = TryReadViewId(note);
                    var resolvedId = SheetViewTypeResolver.ResolveImportId(
                        database,
                        displayedKey,
                        notedId);
                    row[columnIndex] = SheetViewTypeReference.ToLegacyValue(resolvedId);
                }
            }
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

        private static bool IsTypeRow(IReadOnlyList<string> row)
        {
            return row != null
                   && row.Count > 0
                   && string.Equals(row[0], SheetConfig.ROW_TYPE_MARKER, StringComparison.Ordinal);
        }

        private static string TryReadViewId(string note)
        {
            return !string.IsNullOrEmpty(note)
                   && note.StartsWith(NotePrefix, StringComparison.Ordinal)
                ? note.Substring(NotePrefix.Length).Trim()
                : string.Empty;
        }
    }
}
