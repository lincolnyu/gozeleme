using System;

namespace DeDup.Logging
{
    public interface ILogWriter
    {
        Logger.LogLevels LogLevel { get; set; }

        void Write(string s);
    }
}