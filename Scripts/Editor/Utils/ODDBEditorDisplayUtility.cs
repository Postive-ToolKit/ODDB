namespace TeamODD.ODDB.Editors.Utils
{
    internal static class ODDBEditorDisplayUtility
    {
        public static string FormatNameWithId(string name, string id)
        {
            if (string.IsNullOrEmpty(id))
                return name;

            if (string.IsNullOrEmpty(name) || name == id)
                return id;

            return $"{name} - {ShortId(id)}";
        }

        public static string ShortId(string id)
        {
            return string.IsNullOrEmpty(id) || id.Length <= 6 ? id : id.Substring(0, 6);
        }
    }
}
