using System.Linq;
using System.Text.RegularExpressions;

namespace TeamODD.ODDB.Editors.CodeGen
{
    /// <summary>
    /// Validates identifiers used as generated class names and field names.
    /// Structural rules (regex) live here; the C# reserved-word list lives
    /// in Resources/CodeGen/reserved-words.txt and is loaded via TemplateLoader.
    /// </summary>
    internal static class ClassNameValidator
    {
        private static readonly Regex IdentifierRegex = new("^[A-Za-z][A-Za-z0-9]*$", RegexOptions.Compiled);

        public static bool IsValidIdentifier(string name, out string reason)
        {
            if (string.IsNullOrEmpty(name))
            {
                reason = "name is empty";
                return false;
            }
            if (!IdentifierRegex.IsMatch(name))
            {
                reason = $"'{name}' must match ^[A-Za-z][A-Za-z0-9]* (English letters/digits, first char letter)";
                return false;
            }
            if (TemplateLoader.ReservedWords.Contains(name))
            {
                reason = $"'{name}' is a C# reserved word";
                return false;
            }
            reason = null;
            return true;
        }
    }
}
