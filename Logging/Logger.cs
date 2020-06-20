using System;
using System.IO;
using System.Collections.Generic;

namespace Logging
{
    public class Logger
    {
        public enum LogLevels
        {
            Error,
            Warning,
            Verbose
        }

        public HashSet<ILogWriter> LogWriters { get; } = new HashSet<ILogWriter>();

        public void Log(LogLevels logLevel, string s)
        {
            foreach (var lw in LogWriters)
            {
                if (lw.LogLevel >= logLevel)
                {
                    lw.Write(s);
                }
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