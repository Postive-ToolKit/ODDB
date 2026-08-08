using System;

namespace TeamODD.ODDB.Editors.Utils.Sheets
{
    internal static class SheetViewTypeReference
    {
        private const string SpacedPrefix = "view - ";
        private const string SlashPrefix = "view/";
        private const string DisplayPrefix = "view-";

        public static bool TryParse(string value, out string parameter)
        {
            parameter = string.Empty;
            var text = value?.Trim();
            if (string.IsNullOrEmpty(text))
                return false;

            if (text.StartsWith(SpacedPrefix, StringComparison.OrdinalIgnoreCase))
                parameter = text.Substring(SpacedPrefix.Length).Trim();
            else if (text.StartsWith(SlashPrefix, StringComparison.OrdinalIgnoreCase))
                parameter = text.Substring(SlashPrefix.Length).Trim();
            else if (text.StartsWith(DisplayPrefix, StringComparison.OrdinalIgnoreCase))
                parameter = text.Substring(DisplayPrefix.Length).Trim();
            else
                return false;

            return !string.IsNullOrEmpty(parameter);
        }

        public static string ToLegacyValue(string viewId)
        {
            return SpacedPrefix + (viewId ?? string.Empty);
        }

        public static string ToDisplayValue(string viewName)
        {
            return "View-" + (viewName ?? string.Empty);
        }
    }
}
