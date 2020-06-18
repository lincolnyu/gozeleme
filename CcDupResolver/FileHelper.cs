using System;
using System.IO;
using System.Collections.Generic;

namespace CcDupResolver
{
    public static class FileHelper
    {
        static List<string> Split(FileInfo fi)
        {
            var r = Split(fi.Directory);
            r.Add(fi.Name);
            return r;
        }
        static List<string> Split(DirectoryInfo fi)
        {
            var l = new List<string>();
            var p = fi;
            for (; p != null; p = p.Parent)
            {
                l.Add(p.Name);
            }
            l.Reverse();
            return l;
        }

        public static FileInfo ToAbsolute(string targetRelative, DirectoryInfo baseDir)
        {
            // Path.Combine seems to handle the case where targetRelative is absolute 
            return new FileInfo(Path.Combine(baseDir.FullName, targetRelative));
        }

        public static string ToRelative(FileInfo target, DirectoryInfo baseDir)
        {
            var r = new List<string>();
            var spTarget = Split(target);
            var spBase = Split(baseDir);
            var iDiffStart = 0;
            for (; iDiffStart < Math.Min(spTarget.Count, spBase.Count); iDiffStart++)
            {
                var s1 = spTarget[iDiffStart];
                var s2 = spBase[iDiffStart];
                if (s1 != s2)
                {
                    break;
                }
            }
            if (iDiffStart == 0)
            {
                // Different root, returning absolute
                return target.FullName;
            }
            var sl = new List<string>();
            for (var i = spBase.Count-1; i >= iDiffStart; i--)
            {
                sl.Add("..");
            }
            for (var i = iDiffStart; i < spTarget.Count; i++)
            {
                sl.Add($"{spTarget[i]}");
            }
            return string.Join(@"\", sl);
        }
    }
}