using System;
using System.Linq;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Utils.Sheets
{
    internal static class SheetViewTypeResolver
    {
        public static IView FindView(ODDatabase database, string idOrName)
        {
            if (database == null || string.IsNullOrWhiteSpace(idOrName))
                return null;

            return database.GetAll().FirstOrDefault(view =>
                view != null
                && (string.Equals(view.ID.ToString(), idOrName, StringComparison.Ordinal)
                    || string.Equals(view.Name, idOrName, StringComparison.OrdinalIgnoreCase)));
        }

        public static string ResolveImportId(
            ODDatabase database,
            string displayedKey,
            string stableId)
        {
            var stableView = FindView(database, stableId);
            var displayedView = FindView(database, displayedKey);

            if (stableView != null
                && (string.Equals(displayedKey, stableView.ID.ToString(), StringComparison.Ordinal)
                    || string.Equals(displayedKey, stableView.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return stableView.ID.ToString();
            }

            if (displayedView != null)
                return displayedView.ID.ToString();

            // A stale display name is expected after a local View rename. The stable
            // ID remains authoritative when the displayed value cannot be resolved.
            return !string.IsNullOrWhiteSpace(stableId)
                ? stableId.Trim()
                : displayedKey?.Trim() ?? string.Empty;
        }
    }
}
