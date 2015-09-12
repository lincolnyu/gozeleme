using System;
using System.IO;
using FileMatcherLib;

namespace FileMatcherConsole
{
    class Program
    {
        static bool IsInDirectory(FileInfo f, DirectoryInfo d)
        {
            var fpath = f.FullName.ToLower();   // cross-platform implications
            var dpath = d.FullName.ToLower();
            return (fpath.Contains(dpath));
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                var loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var appn = Path.GetFileNameWithoutExtension(loc);
                Console.WriteLine(@"Usage: {0} <folder1> <folder2> ...", appn);
                return;
            }
            var fm = new FileMatcher(args);
            var igs = fm.GetIdenticalFiles(new FileMatchingCanceller());
            var count = 0;

            Console.WriteLine(@"Searching identical files in folders, ");
            foreach (var sd in fm.StartingDirectories)
            {
                Console.WriteLine(@"  {0}", sd);
            }

            foreach (var ig in igs)
            {
                var fileLen = ig.First.Value.Length;
                if (fileLen == 0)
                {
                    Console.WriteLine(@"==== Identical-file group {0:00000}, size empty     , count {1:0000} ===== ",
                                      count++, ig.Count);
                }
                else if (fileLen < 1024)
                {
                    Console.WriteLine(@"==== Identical-file group {0:00000}, size {1:0000} Bytes, count {2:0000} ===== ",
                                      count++, fileLen, ig.Count);
                }
                else if (fileLen < 1024*1024)
                {
                    Console.WriteLine(@"==== Identical-file group {0:00000}, size {1:0000} KB, count {2:0000} ===== ",
                                      count++, (fileLen+512)/1024, ig.Count);
                }
                else
                {
                    Console.WriteLine(@"==== Identical-file group {0:00000}, size {1:0000} MB, count {2:0000} ===== ",
                                      count++, (fileLen + 512*1024)/(1024*1024), ig.Count);
                }
                var lastSdi = -1;
                var folder = "";
                foreach (var f in ig)
                {
                    int sdi;
                    for (sdi = 0; sdi < fm.StartingDirectories.Count; sdi++)
                    {
                        if (IsInDirectory(f, fm.StartingDirectories[sdi]))
                        {
                            break;
                        }
                    }
                    System.Diagnostics.Trace.Assert(sdi >= 0);
                    if (sdi != lastSdi)
                    {
                        folder = fm.StartingDirectories[sdi].FullName;
                        if (folder[folder.Length - 1] != '\\')
                        {
                            folder += '\\';
                        }
                        Console.WriteLine(@"  Folder: {0}", folder);
                        lastSdi = sdi;
                    }
                    Console.WriteLine(@"    {0}", f.FullName.Substring(folder.Length));
                }
            }
        }
    }
}
