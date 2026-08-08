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

        public const string GROUP_MARKER = "#ODDB_GROUP";
        public const string TABLE_MARKER = "#TABLE";
        public const string TABLE_END_MARKER = "#END_TABLE";
        public const string GROUP_END_MARKER = "#END_GROUP";
        public const string GROUP_LAYOUT_VERSION = "1";
    }
}
