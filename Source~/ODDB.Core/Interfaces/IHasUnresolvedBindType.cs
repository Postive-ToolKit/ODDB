namespace TeamODD.ODDB.Runtime.Interfaces
{
    // Keeps an engine-specific binding name intact when its CLR type is absent.
    public interface IHasUnresolvedBindType
    {
        string UnresolvedBindType { get; set; }
    }
}
