using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DeDup.Logging;

namespace DeDup.Core
{
    public class DeDupParameters
    {
        public IEnumerable<DirectoryInfo> Dirs { get; set; }
        public Predicate<DirectoryInfo> ExcludeDir { get; set; }
        public Predicate<FileInfo> IncludeFile { get; set; }
        public Predicate<FileInfo> ExcludeFile { get; set; }
        public ParallelOptions ParallelOptions { get; set; }
        public Logger Logger {get; set;}
    }
}
