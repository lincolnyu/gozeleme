using DeDup.Core;
using Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CcDupList;

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
        const string ArgIncludeCcDup = "--include-ccdup";

        const string ArgQuiet = "--quiet";
        const string ArgQuietAbbr = "-q";

        const string LogLevelError = "ERROR";
        const string LogLevelWarning = "WARNING";
        const string LogLevelVerbose = "VERBOSE";

        static void PrintHelp()
        {
            Console.WriteLine($"[{ArgDir} <dir>...] [{ArgExcludeDir} <excluded_dir>...] "
                + $"[{ArgExcludeFilePattern} <excluded_file_pattern>...] "
                + $"[{ArgIncludeFilePattern} <included_file_pattern>...] "
                + $"[{ArgOutputFile}=<output_file>] [{ArgThreadNum}=<thread_num=0/*System*/|(1)/*Single*/|2|3|4|...>] "
                + $"[{ArgLogLevl}=<log_level={LogLevelError}|({LogLevelWarning})|{LogLevelVerbose}>] "
                + $"[{ArgIncludeCcDup}]=<(0)|1> "
                + $"[{ArgQuiet}|{ArgQuietAbbr}]");
            Console.WriteLine("--help|-h");
        }

        static bool TryGetArgValue(string arg, string argHead, out string sVal, bool checkEqualSign=true)
        {
            sVal = "";
            return TryGetArgValueRef(arg, argHead, ref sVal, checkEqualSign);
        }

        static bool TryGetArgValueRef(string arg, string argHead, ref string sVal, bool checkEqualSign=true)
        {
            if (!arg.StartsWith(argHead))
            {
                return false;
            }
            if (argHead.Length < arg.Length)
            {
                if (arg[argHead.Length]=='=')
                {
                    sVal = arg.Substring(argHead.Length+1).Trim();
                    return true;   
                }
            }
            else if (checkEqualSign)
            {
                throw new ArgumentException($"{arg} expected to be followed by '='.");
            }
            return false;
        }

        static void Main(string[] args)
        {
            string leadingArg = null;
            var ddPar = new DeDuper.Parameters();
            var dirs = new List<DirectoryInfo>();
            var exclDirs = new HashSet<string>();
            var exclFilePatterns = new List<string>();
            var inclFilePatterns = new List<string>();
            var threadNum = 1;
            string outputFileName = null;
            var includeCcDup = false; 
            var quiet = false;
            var consoleLogWritter = new ConsoleLogWriter();
            var logger = new Logger();
            logger.LogWriters.Add(consoleLogWritter);

            logger.Info($"\rParsing args...");
            try
            {
                foreach (var arg in args)
                {
                    switch (leadingArg)
                    {
                        case null:
                            if (TryGetArgValue(arg, ArgThreadNum, out var sThreadNum))
                            {
                                if (!int.TryParse(sThreadNum, out threadNum))
                                {
                                    throw new ArgumentException($"Invalid {ArgThreadNum} value. Integer expected instead of '{sThreadNum}'.");
                                }
                            }
                            else if (TryGetArgValue(arg, ArgLogLevl, out var sLogLevel))
                            {
                                switch (sLogLevel.ToUpper())
                                {
                                    case LogLevelError:
                                        consoleLogWritter.LogLevel = Logger.LogLevels.Error;
                                        break;
                                    case LogLevelWarning:
                                        consoleLogWritter.LogLevel = Logger.LogLevels.Warning;
                                        break;
                                    case LogLevelVerbose:
                                        consoleLogWritter.LogLevel = Logger.LogLevels.Verbose;
                                        break;
                                    default:
                                        throw new ArgumentException($"Invalid {ArgLogLevl} value: '{sLogLevel}'.");
                                }
                            }
                            else if (TryGetArgValue(arg, ArgIncludeCcDup, out var sIncludeCcDup))
                            {
                                switch (sIncludeCcDup.ToUpper())
                                {
                                    case "0":
                                    case "FALSE":
                                        includeCcDup = false;
                                        break;
                                    case "1":
                                    case "TRUE":
                                        includeCcDup = true;
                                        break;
                                    default:
                                        throw new ArgumentException($"Invalid {ArgIncludeCcDup} value: '{sLogLevel}'.");
                                }
                            }
                            else if (TryGetArgValueRef(arg, ArgOutputFile, ref outputFileName))
                            {
                            }
                            else
                            {
                                switch (arg)
                                {
                                    case ArgQuiet:
                                    case ArgQuietAbbr:
                                        quiet = true;
                                        break;                                    
                                    case "--help":
                                    case "-h":
                                        PrintHelp();
                                        return;
                                    default:
                                        leadingArg = arg;
                                        break;
                                }
                            }
                            break;
                        case ArgDir:
                            if (Directory.Exists(arg))
                            {
                                dirs.Add(new DirectoryInfo(arg));
                            }
                            leadingArg = null;
                            break;
                        case ArgExcludeDir:
                            if (Directory.Exists(arg))
                            {
                                exclDirs.Add(arg);
                            }
                            leadingArg = null;
                            break;
                        case ArgExcludeFilePattern:
                            exclFilePatterns.Add(arg);
                            leadingArg = null;
                            break;
                        case ArgIncludeFilePattern:
                            inclFilePatterns.Add(arg);
                            leadingArg = null;
                            break;
                        default:
                            throw new ArgumentException($"Unexpected arg {leadingArg}.");
                    }
                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"Argument Error: {e.Message}");
                PrintHelp();
                return;
            }

            var outputToFile = outputFileName != null;
            if (outputToFile)
            {
                if (File.Exists(outputFileName))
                {
                    if (!quiet)
                    {
                        Console.Write($"'{outputFileName}' already exists. Continue (Y)?");
                        var r = Console.ReadLine();
                        if (r.ToUpper() != "Y")
                        {
                            Console.WriteLine($"Cancelled by user.");
                            return;
                        }
                    }
                    logger.WarningLine($"Warning: '{outputFileName}' already exists, overwritten.");
                }
                else
                {
                    var dir = Path.GetDirectoryName(outputFileName);
                    if (!Directory.Exists(dir))
                    {
                        logger.ErrorLine("Error: directory for '{outputFileName}' does not exist.");
                        return;
                    }
                }
            }

            ddPar.Logger = logger;
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
            ddPar.ExcludeFile = (exclFilePatterns.Count > 0 || !includeCcDup)? new Predicate<FileInfo>(
                f=>
                {
                    if (!includeCcDup && f.Extension == ".ccdup")
                    {
                        return false;
                    }
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

            logger.InfoLine($"\rParsing args done.");
            logger.InfoLine($"Threads: {ddPar.ParallelOptions?.MaxDegreeOfParallelism?? 1}");

            var dd = new DeDuper(ddPar);
            var output = outputToFile? new StreamWriter(outputFileName) : Console.Out;
            var writer = new Writer<DdFile>(output);
            long totalDupSize = 0;
            var totalDupFiles = 0;
            writer.Run(dd.DupFileGroups, out totalDupSize, out totalDupFiles);

            // using var output = outputToFile?
            //     new StreamWriter(outputFileName) : Console.Out;
            // long totalDupSize = 0;
            // var totalDupFiles = 0;
            // foreach (var ddg in dd.DupFileGroups)
            // {
            //     output.WriteLine(outputToFile? CcBar : ConsoleBar);
            //     var dupSize = ddg.FileLength * (ddg.Files.Count-1);
            //     totalDupSize += dupSize;
            //     totalDupFiles += ddg.Files.Count-1;
            //     foreach (var f in ddg.Files)
            //     {
            //         output.WriteLine($"{f.File.Name}\t{f.File.DirectoryName}\t{f.FileLength.StringifyFileLength()}\t{f.File.CreationTime}");
            //     }                
            // }

            var majorSeparator = outputToFile? CcBar : ConsoleBar;
            if (dd.FailedFiles.Count > 0)
            {
                output.WriteLine(majorSeparator.Replace('-', '='));
                output.WriteLine($"Failed to read {dd.FailedFiles.Count} files.");
                foreach (var ff in dd.FailedFiles.OrderBy(x=>x.File.FullName))
                {
                    output.WriteLine(ff.File.FullName);
                }
            }

            var summary = $"{totalDupFiles} duplicate files in {totalDupSize.StringifyFileLength()}.";
            if (outputToFile)
            {
                output.WriteLine(majorSeparator.Replace('-', '='));
                output.WriteLine(summary);
            }
            logger.InfoLine(summary);
        }
    }
}
