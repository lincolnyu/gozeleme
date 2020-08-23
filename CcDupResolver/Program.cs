using Logging;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace CcDupResolver
{
    class Program
    {
        const string Bar = "------------------------------------------------------------------------------------------------------------------------------------------------------";

        static void ResolvePreferringShortest(string ccDupListFileName, Logger logger)
        {
            using var ccDupList = new StreamReader(ccDupListFileName);
            using var fLog = new StreamWriter("log.txt");
            var parser = new Parser(ccDupList, logger);

            var groups = parser.Run();
            foreach (var g in groups)
            {
                var sel = g.Files.Values.OrderBy(x=>x.Dir.Length).First();
                g.Resolve(sel);
            }
        }

        static (int, int ,int) Fix(FileInfo ccdup)
        {
            var nonexistentCount = 0;
            var absToRelCount = 0;
            var copiedCount = 0;
            var refs = new List<string>();
            var ccdupDir = ccdup.Directory;
            {
                using var ccdupfs = ccdup.Open(FileMode.Open);
                using var ccdupr = new StreamReader(ccdupfs);
                while (!ccdupr.EndOfStream)
                {
                    // Update the absolute to relative and remove non-existent
                    var l = ccdupr.ReadLine();
                    if (!File.Exists(l))
                    {
                        nonexistentCount++;
                        continue;
                    }
                    if (Path.IsPathRooted(l))
                    {
                        refs.Add(FileHelper.ToRelative(new FileInfo(l), ccdupDir));
                        absToRelCount++;
                    }
                    else
                    {
                        refs.Add(l);
                        copiedCount++;
                    }
                }
            }
            {
                using var ccdupfs = ccdup.Open(FileMode.Create);
                using var ccdupw = new StreamWriter(ccdupfs);
                foreach (var r in refs)
                {
                    ccdupw.WriteLine(r);
                }
            }
            return (nonexistentCount, absToRelCount, copiedCount);
        }

        static void FixCcDupInDir(string dirStr)
        {
            var dir = new DirectoryInfo(dirStr);
            var ccdupList = dir.GetFiles("*.ccdup", SearchOption.AllDirectories);
            var itemsChanged = 0;
            var totalNonexistent = 0;
            var totalAbsToRel = 0;
            var totalCopied = 0;
            foreach (var ccdup in ccdupList)
            {
                var (nonexistentCount, absToRelCount, copiedCount) = Fix(ccdup);
                totalNonexistent += nonexistentCount;
                totalAbsToRel += absToRelCount;
                totalCopied += copiedCount;
            }
            Console.WriteLine($"{ccdupList.Length} ccdup files fixed with {totalNonexistent} nonexistent items removed and {totalAbsToRel} absolute to relative.");
        }

        // Moves folder sourceFolder to targetFolder, change all affected ccdup file accordingly
        // with ccDupListFileName provided pointing to all the related ccdup files
        static void MoveFolder(string ccDupListFileName, DirectoryInfo sourceFolder, 
            DirectoryInfo targetFolder, Logger logger)
        {
            using var ccDupList = new StreamReader(ccDupListFileName);
            using var fLog = new StreamWriter("log.txt");
            var parser = new Parser(ccDupList, logger);

            var groups = parser.Run().ToList();
            foreach (var g in groups)
            {
                foreach (var f in g.Files.Values)
                {
                    //f.FullName
                }
            }
            foreach (var g in groups)
            {
                
            }
        }

        // Moves folder sourceFolder to targetFolder, change all affected ccdup file accordingly
        // with all the directories to search provided
        static void MoveFolder(ICollection<DirectoryInfo> directories, DirectoryInfo sourceFolder, 
            DirectoryInfo targetFolder)
        {
            foreach (var dir in directories)
            {

            }
        }

        static void MoveFolder(string sourceFolder, string targetFolder, string ccdupListFile, Logger logger)
        {
            var sourceDir = new DirectoryInfo(sourceFolder);
            var targetDir = targetFolder != null? new DirectoryInfo(targetFolder) : null;
            if (ccdupListFile != null)
            {
                MoveFolder(ccdupListFile, sourceDir, targetDir, logger);
            }
            else
            {
                var dirs = targetDir != null? new DirectoryInfo[]{
                    sourceDir,
                    targetDir
                } : new DirectoryInfo[] {
                    sourceDir
                };
                MoveFolder(dirs, sourceDir, targetDir);
            }
            Directory.Move(sourceFolder, targetFolder);
        }

        static void PrintHelp()
        {
            Console.WriteLine("<ccduplistfile> --resolve");
            Console.WriteLine("<dir> --fix");
            Console.WriteLine("<dir1> [<dir2>] [<ccduplistfile>] --move // absence of <dir2> meaning deleting <dir1>");
        }

        enum Commands
        {
            None,
            Resolve,
            Fix,
            Move
        }

        static void Main(string[] args)
        {
            var consoleLogWritter = new ConsoleLogWriter();
            var logger = new Logger();
            logger.LogWriters.Add(consoleLogWritter);

            try
            {
                string argAsFile = null;
                string argAsDir = null;
                string argAsDir2 = null;
                var cmd = Commands.None;
                foreach (var arg in args)
                {
                    if (arg == "--resolve")
                    {
                        cmd = Commands.Resolve;
                    }
                    else if (arg == "--fix")
                    {
                        cmd = Commands.Fix;
                    }
                    else if (arg == "--move")
                    {
                        // move a folder and change all the affected ccdup
                        cmd = Commands.Move;
                    }
                    else if (arg == "--help")
                    {
                        PrintHelp();
                        return;
                    }
                    else if (Directory.Exists(arg))
                    {
                        if (argAsDir != null)
                        {
                            argAsDir = arg;
                        }
                        else if (argAsDir2 != null)
                        {
                            argAsDir2 = arg;
                        }
                    }
                    else if (File.Exists(arg))
                    {
                        argAsFile = arg;
                    }
                }
                
                switch (cmd)
                {
                    case Commands.Resolve:
                        if (argAsFile == null)
                        {
                            throw new ArgumentException("'Resolve' command expects a valid file name.");
                        }
                        ResolvePreferringShortest(argAsFile, logger);
                        break;
                    case Commands.Fix:
                        if (argAsDir == null)
                        {
                            throw new ArgumentException("'Fix' command expects a valid directory name.");
                        }
                        FixCcDupInDir(argAsDir);
                        break;
                    case Commands.Move:
                        if (argAsDir == null)
                        {
                            throw new ArgumentException("'Move' command expects at least one valid directory name.");
                        }
                        MoveFolder(argAsDir, argAsDir2, argAsFile, logger);
                        break;
                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"Argument Error: {e.Message}");
                PrintHelp();
                return;
            }
        }
    }
}
