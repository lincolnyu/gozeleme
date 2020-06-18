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

        static void Resolve(string ccDupListFileName)
        {
            using var ccDupList = new StreamReader(ccDupListFileName);
            using var fLog = new StreamWriter("log.txt");
            var dupGroup = new List<DupFile>();
            var iLine = 0;
            var error = false;
            var totalFilesRemoved = 0;
            long totalBytesRemoved = 0;

            void proc()
            {
                if (!error)
                {
                    if (dupGroup.Count < 2)
                    {
                        if (iLine > 0)
                        {
                            fLog.WriteLine($"Line {iLine}: Concluding a non-duplicate file group.");
                        }
                        return;
                    }

                    var dupGroupSorted = dupGroup.OrderBy(x=>x.Dir.Length).ToArray();
                    var baseDir = dupGroupSorted[0].Dir;
                    var dupsFlattened = new List<string>();
                    for (var i = 1; i < dupGroupSorted.Length; i++)
                    {
                        if (File.Exists(dupGroupSorted[i].CcDupFullPath))
                        {
                            // The file is a representative 
                            using var ccdupir = new StreamReader(dupGroupSorted[i].CcDupFullPath);
                            while (!ccdupir.EndOfStream)
                            {
                                var origDup = ccdupir.ReadLine();
                                var origDupDir = Path.GetDirectoryName(origDup);
                                if (!Path.IsPathRooted(origDup))
                                {
                                    origDup = FileHelper.ToAbsolute(origDup, 
                                        new DirectoryInfo(dupGroupSorted[i].Dir)).FullName;
                                }
                                var origCcDup = DupFile.ToCcDup(origDup);
                                if (File.Exists(origCcDup))
                                {
                                    using var ccdpic = new StreamWriter(origCcDup);
                                    ccdpic.WriteLine($"{FileHelper.ToRelative(new FileInfo(dupGroupSorted[0].FullPath), new DirectoryInfo(origDupDir))}");
                                    dupsFlattened.Add(origDup);
                                }
                                else
                                {
                                    fLog.WriteLine($"Line {iLine}: original placeholder file '{origCcDup}' not found.");
                                }
                            }
                        }
                        using var ccdupiw = new StreamWriter(dupGroupSorted[i].CcDupFullPath);
                        ccdupiw.WriteLine($"{FileHelper.ToRelative(new FileInfo(dupGroupSorted[0].FullPath), new DirectoryInfo(dupGroupSorted[i].Dir))}");
                        totalBytesRemoved += dupGroupSorted[i].FileLength;
                        dupsFlattened.Add(dupGroupSorted[i].FullPath);
                        File.Delete(dupGroupSorted[i].FullPath);
                    }

                    using var ccdupMaster = new StreamWriter(dupGroupSorted[0].CcDupFullPath);
                    foreach (var dup in dupsFlattened)
                    {
                        ccdupMaster.WriteLine($"{FileHelper.ToRelative(new FileInfo(dup), new DirectoryInfo(dupGroupSorted[0].Dir))}");
                    }
                    totalFilesRemoved += dupGroupSorted.Length-1;
                }
                dupGroup.Clear();
            }

            for (; !ccDupList.EndOfStream; iLine++)
            {
                var l = ccDupList.ReadLine();
                if (l == Bar)
                {
                    proc();
                    continue;
                }
                var segs = l.Split('\t');
                if (segs.Length < 2)
                {
                    fLog.WriteLine($"Line {iLine}: '{l}' Bad format.");
                    error = true;
                    continue;
                }
                var fn = segs[0];
                var dir = segs[1];
                var dupFile = new DupFile
                {
                    Dir = dir,
                    File = fn,
                };
                if (!File.Exists(dupFile.FullPath))
                {
                    fLog.WriteLine($"Line {iLine}: File '{dupFile.FullPath}' not found.");
                    error = true;
                    continue;
                }
                dupGroup.Add(dupFile);
            }
            proc();
            fLog.WriteLine($"Totally {totalBytesRemoved} bytes ({totalFilesRemoved} files) removed.");
            Console.WriteLine($"Totally {totalBytesRemoved} bytes ({totalFilesRemoved} files) removed.");
        }

        static void UpgradeToRelativePath(string dirStr)
        {
            var dir = new DirectoryInfo(dirStr);
            var ccdupList = dir.GetFiles("*.ccdup", SearchOption.AllDirectories);
            var itemsChanged = 0;
            foreach (var ccdup in ccdupList)
            {
                var refs = new List<string>();
                var ccdupDir = ccdup.Directory;
                {
                    using var ccdupfs = ccdup.Open(FileMode.Open);
                    using var ccdupr = new StreamReader(ccdupfs);
                    while (!ccdupr.EndOfStream)
                    {
                        var l = ccdupr.ReadLine();
                        if (Path.IsPathRooted(l))
                        {
                            refs.Add(FileHelper.ToRelative(new FileInfo(l), ccdupDir));
                            itemsChanged++;
                        }
                        else
                        {
                            refs.Add(l);
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
            }
            Console.WriteLine($"{ccdupList.Length} ccdup files upgraded with {itemsChanged} items changed.");
        }

        static void Main(string[] args)
        {
            if (Directory.Exists(args[0]))
            {
                UpgradeToRelativePath(args[0]);
            }
            else if (File.Exists(args[0]))
            {
                Resolve(args[0]);
            }
            else
            {
                Console.WriteLine("Usage: <file> CC duplicate file list");
                Console.WriteLine("       <dir>  Make sure all ccdup files use relative directories");
            }
        }
    }
}
