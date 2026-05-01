namespace TeamODD.ODDB.Editors.CodeGen
{
    /// <summary>
    /// One human-readable validation failure surfaced by the code generator.
    /// Aggregated and shown to the user via ODDBResultWindow.
    /// </summary>
    internal readonly struct ValidationError
    {
        public string ViewName { get; }
        public string FieldName { get; }
        public string Reason { get; }

        public ValidationError(string viewName, string fieldName, string reason)
        {
            ViewName = viewName ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public static ValidationError ForView(string viewName, string reason)
            => new ValidationError(viewName, null, reason);

        public static ValidationError ForField(string viewName, string fieldName, string reason)
            => new ValidationError(viewName, fieldName, reason);

        public string ToDisplayLine()
        {
            return string.IsNullOrEmpty(FieldName)
                ? $"[{ViewName}] {Reason}"
                : $"[{ViewName}] field '{FieldName}': {Reason}";
        }
    }
}
