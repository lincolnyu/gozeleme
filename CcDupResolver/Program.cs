using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CcDupResolver
{
    class Program
    {
        class DupFile
        {
            public string Dir;
            public string File;
            public string FullPath => Path.Combine(Dir, File);
        }

        static void Main(string[] args)
        {
            const string Bar = "------------------------------------------------------------------------------------------------------------------------------------------------------";
            using var f = new StreamReader(args[0]);
            using var fLog = new StreamWriter("log.txt");
            var dup = new List<DupFile>();
            var iLine = 0;
            var error = false;
            for (; !f.EndOfStream; iLine++)
            {
                var l = f.ReadLine();
                if (l == Bar)
                {
                    if (!error)
                    {
                        var a = dup.OrderBy(x=>x.Dir.Length).ToArray();
                        for (var i = 1; i < a.Length; i++)
                        {
                            var ddfn = Path.Combine(a[i].Dir, a[i].File + "-duplicate.txt");
                            using var dfile = new StreamWriter(ddfn);
                            dfile.WriteLine($"{Path.Combine(a[0].Dir, a[0].File)}");
                            File.Delete(a[i].FullPath);
                        }
                    }
                    dup.Clear();
                }
                var segs = l.Split('\t');
                if (segs.Length < 2)
                {
                    fLog.WriteLine($"Line {iLine} bad format.");
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
                    fLog.WriteLine($"Line {iLine} file '{dupFile.FullPath}' not found.");
                    error = true;
                    continue;
                }
                dup.Add(dupFile);
            }
        }
    }
}
