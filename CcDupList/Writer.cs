using Logging;
using System;
using System.IO;
using System.Collections.Generic;

namespace CcDupList
{
    public static class WriterConstants
    {
        public const string ConsoleBar = "--------------------------------------------------------------------------------";
        public const string CcBar = "------------------------------------------------------------------------------------------------------------------------------------------------------";
    }

    public class Writer<TDupFile> where TDupFile : IDupFile
    {
        
        TextWriter _tw;
        bool _outputToConsole;
        public Writer(TextWriter tw)
        {
            _tw = tw;
            _outputToConsole = tw == Console.Out;
        }
        public void Run(IEnumerable<IDupFileGroup<TDupFile>> groups, out long totalDupSize, out int totalDupFiles, string outputFileName = null)
        {
            totalDupSize = 0;
            totalDupFiles = 0;
            foreach (var ddg in groups)
            {
                _tw.WriteLine(_outputToConsole? WriterConstants.ConsoleBar : WriterConstants.CcBar);
                var dupSize = ddg.FileLength * (ddg.Files.Count-1);
                totalDupSize += dupSize;
                totalDupFiles += ddg.Files.Count-1;
                foreach (var f in ddg.Files)
                {
                    _tw.WriteLine($"{f.File.Name}\t{f.File.DirectoryName}\t{ddg.FileLength.StringifyFileLength()}\t{f.File.CreationTime}");
                }                
            }
        }
    }
}