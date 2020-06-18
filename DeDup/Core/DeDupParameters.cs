using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DeDup.Core
{
    public class DeDupParameters
    {
        public enum LogLevels
        {
            Error,
            Warning,
            Verbose
        }
        public IEnumerable<DirectoryInfo> Dirs { get; set; }
        public Predicate<DirectoryInfo> ExcludeDir { get; set; }
        public Predicate<FileInfo> IncludeFile { get; set; }
        public Predicate<FileInfo> ExcludeFile { get; set; }
        public ParallelOptions ParallelOptions { get; set; }

        public LogLevels LogLevel {get; set;} = LogLevels.Warning;
    }
}
