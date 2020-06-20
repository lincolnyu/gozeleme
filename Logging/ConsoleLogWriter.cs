using System;

namespace Logging
{
    public class ConsoleLogWriter : ILogWriter
    {
        public Logger.LogLevels LogLevel { get; set; } = Logger.LogLevels.Verbose;

        public void Write(string s)
        {
            Console.Write(s);
        }
    }
}
