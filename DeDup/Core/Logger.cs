using System;
using System.IO;

namespace DeDup.Core
{
    public class Logger
    {
        public enum LogLevels
        {
            Error,
            Warning,
            Verbose
        }

        public LogLevels LogLevel { get; set; } = LogLevels.Warning;

        public void Log(LogLevels logLevels, string s)
        {
            if (LogLevel >= logLevels)
            {
                Console.Write(s);
            }
        }
        public void Info(string s)
        {
            Log(LogLevels.Verbose, s);
        }
        public void InfoLine(string s)
        {
            Log(LogLevels.Verbose, s+"\n");
        }
        public void ErrorLine(string s)
        {
            Log(LogLevels.Error, s+"\n");
        }
        public void WarningLine(string s)
        {
            Log(LogLevels.Warning, s+"\n");
        }
    }
}