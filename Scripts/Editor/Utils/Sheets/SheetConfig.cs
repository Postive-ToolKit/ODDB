namespace TeamODD.ODDB.Editors.Utils.Sheets
{
    /// <summary>
    /// Configuration for sheet processing
    /// </summary>
    public static class SheetConfig
    {
        /// <summary>
        /// Ignore mark for sheet or row
        /// </summary>
        public const string IGNORE_PREFIX = "#";

        public const string ROW_NAME_MARKER = "#NAME";
        public const string ROW_TYPE_MARKER = "#TYPE";
        public const string ROW_COMMENT_PREFIX = "#";
        public const string LEGACY_HEADER_FIRST_CELL = "ID";
    }
}