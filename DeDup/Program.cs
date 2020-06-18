using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DeDup.Core;

namespace DeDup
{
    class Program
    {
        const string ConsoleBar = "--------------------------------------------------------------------------------";
        const string CcBar = "------------------------------------------------------------------------------------------------------------------------------------------------------";
        const string ArgDir = "--dir";
        const string ArgExcludeDir = "--exclude-dir";
        const string ArgExcludeFilePattern = "--exclude-file-pattern";
        const string ArgIncludeFilePattern = "--include-file-pattern";
        const string ArgOutputFile = "--output";
        const string ArgThreadNum = "--threads";
        const string ArgLogLevl = "--log-level";

        const string LogLevelError = "ERROR";
        const string LogLevelWarning = "WARNING";
        const string LogLevelVerbose = "VERBOSE";

        static void PrintHelp()
        {
            Console.WriteLine($"[{ArgDir} <dir>...] [{ArgExcludeDir} <excluded_dir>...] "
                + "[{ArgExcludeFilePattern} <excluded_file_pattern>...] "
                + "[{ArgIncludeFilePattern} <included_file_pattern>...] "
                + "[{ArgOutputFile} <output_file>] [{ArgThreadNum} <thread_num=0/*System*/,(1)/*Single*/,2,3,4...>] "
                + $"[{ArgLogLevl} <log_level={LogLevelError}|({LogLevelWarning})|{LogLevelVerbose}>]");
            Console.WriteLine("--help|-h");
        }
        
        static string StringifyFileLength(long fileLength)
        {
            const long OneKB = 1024; 
            const long OneMB = OneKB * 1024;
            const long OneGB = OneMB * 1024;
            const long OneTB = OneGB * 1024;
            var scales = new []{OneKB, OneMB, OneGB, OneTB};
            var suffices = new []{"KB", "MB", "GB", "TB"};

            long valRound = 0;
            for (var i = 0; i < scales.Length; i++)
            {
                var scale = scales[i];
                var suffix = suffices[i];
                var val = (double)fileLength / scale;
                if (val < 10)
                {
                    return $"{val:0.00} {suffix}";
                }
                else if (val < 100)
                {
                    return $"{val:00.0} {suffix}";
                }
                valRound = (long)Math.Round(val);
                if (valRound < 1000)
                {
                    return $"{valRound} {suffix}";
                }
            }
            return $"{valRound} {suffices[suffices.Length-1]}";
        }

        static void Main(string[] args)
        {
            string activeArg = null;
            var ddPar = new DeDupParameters();
            var dirs = new List<DirectoryInfo>();
            var exclDirs = new HashSet<string>();
            var exclFilePatterns = new List<string>();
            var inclFilePatterns = new List<string>();
            var threadNum = 1;
            string outputFileName = null;
            foreach (var arg in args)
            {
                switch (activeArg)
                {
                    case ArgDir:
                        if (Directory.Exists(arg))
                        {
                            dirs.Add(new DirectoryInfo(arg));
                        }
                        activeArg = null;
                        break;
                    case ArgExcludeDir:
                        if (Directory.Exists(arg))
                        {
                            exclDirs.Add(arg);
                        }
                        activeArg = null;
                        break;
                    case ArgExcludeFilePattern:
                        exclFilePatterns.Add(arg);
                        activeArg = null;
                        break;
                    case ArgIncludeFilePattern:
                        inclFilePatterns.Add(arg);
                        activeArg = null;
                        break;
                    case ArgOutputFile:
                        outputFileName = arg;
                        activeArg = null;
                        break;
                    case ArgThreadNum:
                        if (!int.TryParse(arg, out threadNum))
                        {
                            Console.WriteLine($"Error: Invalid {ArgThreadNum} value: {arg}.");
                            PrintHelp();
                            return;
                        }
                        activeArg = null;
                        break;
                    case ArgLogLevl:
                        switch (arg.ToUpper())
                        {
                            case LogLevelError:
                                ddPar.LogLevel = DeDupParameters.LogLevels.Error;
                                break;
                            case LogLevelWarning:
                                ddPar.LogLevel = DeDupParameters.LogLevels.Warning;
                                break;
                            case LogLevelVerbose:
                                ddPar.LogLevel = DeDupParameters.LogLevels.Verbose;
                                break;
                            default:
                                Console.WriteLine($"Error: Invalid {ArgLogLevl} value: {arg}.");
                                PrintHelp();
                                return;
                        }
                        activeArg = null;
                        break;
                    case null:
                        switch (arg)
                        {
                            case "--help":
                            case "-h":
                                PrintHelp();
                                return;
                        }
                        activeArg = arg;
                        break;
                    default:
                        Console.WriteLine($"Error: Unexepcted arg {activeArg}");
                        PrintHelp();
                        return;
                }
            }

            ddPar.Dirs = dirs;

            ddPar.ExcludeDir = exclDirs.Count > 0? new Predicate<DirectoryInfo>(
                d=>exclDirs.Contains(d.FullName)
            ) : null;
            ddPar.IncludeFile = inclFilePatterns.Count > 0? new Predicate<FileInfo>(
                f=>
                {
                    foreach (var ifp in inclFilePatterns)
                    {
                        var res = Regex.Matches(f.Name, ifp);
                        if (res.Count == 1)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            ) : null;
            ddPar.ExcludeFile = exclFilePatterns.Count > 0? new Predicate<FileInfo>(
                f=>
                {
                    foreach (var ifp in exclFilePatterns)
                    {
                        var res = Regex.Matches(f.Name, ifp);
                        if (res.Count != 1)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            ) : null;

            ddPar.ParallelOptions = threadNum != 1? 
                new ParallelOptions{MaxDegreeOfParallelism = threadNum != 0? 
                     threadNum : Environment.ProcessorCount } 
                : null;

            Console.WriteLine($"Threads: {ddPar.ParallelOptions?.MaxDegreeOfParallelism?? 1}");

            var dd = new DeDuper(ddPar);

            var outputToFile = outputFileName != null;
            using var output = outputToFile?
                new StreamWriter(outputFileName) : Console.Out;
            foreach (var ddg in dd.DupFileGroups)
            {
                output.WriteLine(outputToFile? CcBar : ConsoleBar);
                foreach (var f in ddg.Files)
                {
                    output.WriteLine($"{f.File.Name}\t{f.File.DirectoryName}\t{StringifyFileLength(f.FileLength)}\t{f.File.CreationTime}");
                }                
            }

            if (dd.FailedFiles.Count > 0)
            {
                var bar = outputToFile? CcBar : ConsoleBar;
                output.WriteLine(bar.Replace('-', '='));
                output.WriteLine($"Failed to read {dd.FailedFiles.Count} files.");
                foreach (var ff in dd.FailedFiles.OrderBy(x=>x.File.FullName))
                {
                    output.WriteLine(ff.File.FullName);
                }
            }
        }
    }
}
