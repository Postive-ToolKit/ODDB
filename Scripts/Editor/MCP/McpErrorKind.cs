namespace TeamODD.ODDB.Editors.MCP
{
    public enum McpErrorKind
    {
        NotFound,
        InvalidArg,
        Conflict,
        CodegenFailed,
        SaveFailed,
        Internal,
    }

    public static class McpErrorKindExtensions
    {
        public static string ToWireString(this McpErrorKind kind)
        {
            switch (kind)
            {
                case McpErrorKind.NotFound: return "NOT_FOUND";
                case McpErrorKind.InvalidArg: return "INVALID_ARG";
                case McpErrorKind.Conflict: return "CONFLICT";
                case McpErrorKind.CodegenFailed: return "CODEGEN_FAILED";
                case McpErrorKind.SaveFailed: return "SAVE_FAILED";
                case McpErrorKind.Internal:
                default: return "INTERNAL";
            }
        }
    }
}
