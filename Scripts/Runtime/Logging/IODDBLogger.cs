namespace TeamODD.ODDB.Runtime.Logging
{
    public interface IODDBLogger
    {
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
    }
}
