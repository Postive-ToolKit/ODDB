using TeamODD.ODDB.Runtime.Logging;

namespace TeamODD.ODDB.Runtime
{
    public static class ODDB
    {
        public static IODDBLogger Logger { get; set; } = new NullLogger();
    }
}
