using System;

namespace TeamODD.ODDB.Runtime.Logging
{
    public class ConsoleLogger : IODDBLogger
    {
        public void Info(string msg)  => Console.WriteLine($"[ODDB] {msg}");
        public void Warn(string msg)  => Console.WriteLine($"[ODDB][WARN] {msg}");
        public void Error(string msg) => Console.Error.WriteLine($"[ODDB][ERR ] {msg}");
    }
}
