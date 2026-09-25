namespace TeamODD.ODDB.Editors.CLI
{
    public enum CliErrorKind
    {
        NotFound,
        InvalidArg,
        Conflict,
        CodegenFailed,
        SaveFailed,
        Internal,
    }

    public static class CliErrorKindExtensions
    {
        public static string ToWireString(this CliErrorKind kind)
        {
            switch (kind)
            {
                case CliErrorKind.NotFound: return "NOT_FOUND";
                case CliErrorKind.InvalidArg: return "INVALID_ARG";
                case CliErrorKind.Conflict: return "CONFLICT";
                case CliErrorKind.CodegenFailed: return "CODEGEN_FAILED";
                case CliErrorKind.SaveFailed: return "SAVE_FAILED";
                case CliErrorKind.Internal:
                default: return "INTERNAL";
            }
        }
    }
}

namespace TeamODD.ODDB.Editors.CLI
{
    public sealed class CliOperationException : System.Exception
    {
        public CliErrorKind Kind { get; }
        public object Details { get; }

        public CliOperationException(CliErrorKind kind, string message, object details = null) : base(message)
        {
            Kind = kind;
            Details = details;
        }
    }
}
