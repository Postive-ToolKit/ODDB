namespace TeamODD.ODDB.Runtime.Logging
{
    public class NullLogger : IODDBLogger
    {
        public void Info(string msg) { }
        public void Warn(string msg) { }
        public void Error(string msg) { }
    }
}
