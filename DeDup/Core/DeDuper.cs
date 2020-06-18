using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DeDup.Core
{
    public class DeDuper
    {
        private DeDupParameters _parameters;
        public DeDuper(DeDupParameters parameters)
        {
            _parameters = parameters;
            var dict = new Dictionary<long, DdFileGroup>();
            var files = CollectFiles().Select(x=>new DdFile(x));
            var fileCount = 0;
            foreach(var f in files)
            {
                if (!dict.TryGetValue(f.FileLength, out var v))
                {
                    v = new DdFileGroup();
                    dict[f.FileLength] = v;
                }
                v.AddFile(f);
                Info($"\r{++fileCount} files added...");
            }
            InfoLine($"\rAdding files done.                              ");

            var groupCount = 0;
            if (_parameters.ParallelOptions != null)
            {
                Parallel.ForEach(dict.Values,
                    _parameters.ParallelOptions, 
                    dfg=>
                    {
                        DupFileGroups.AddRange(dfg.Split());
                        lock(this)
                        {
                            Info($"\r{++groupCount}/{dict.Count} groups split...");
                        }
                    });
            }
            else
            {
                foreach (var dfg in dict.Values)
                {
                    DupFileGroups.AddRange(dfg.Split());
                    Info($"\r{++groupCount}/{dict.Count} groups split...");
                }
            }
            InfoLine($"\rSplitting groups done.                              ");

            Info($"\rSorting groups...");
            DupFileGroups.Sort((a,b)=>b.Length.CompareTo(a.Length));
            InfoLine($"\rSorting groups done.                              ");
        }
        public List<DdFileGroup> DupFileGroups { get; } = new List<DdFileGroup>();
        private void Log(DeDupParameters.LogLevels logLevels, string s)
        {
            if (_parameters.LogLevel >= logLevels)
            {
                Console.Write(s);
            }
        }
        private void Info(string s)
        {
            Log(DeDupParameters.LogLevels.Verbose, s);
        }
        private void InfoLine(string s)
        {
            Log(DeDupParameters.LogLevels.Verbose, s+"\n");
        }
        private IEnumerable<FileInfo> CollectFiles()
        {
            IEnumerable<FileInfo> TraverseFilesRecursive(DirectoryInfo dir)
            {
                FileInfo[] files = null;
                try
                {
                    files = dir.GetFiles();
                }
                catch (System.UnauthorizedAccessException)
                {
                }
                if (files != null)
                {
                    foreach (var f in files)
                    {
                        if (_parameters.IncludeFile?.Invoke(f)?? true)
                        {
                            if (!(_parameters.ExcludeFile?.Invoke(f)?? false))
                            {
                                yield return f;
                            }
                        }
                    }
                }

                DirectoryInfo[] subdirs = null;
                try
                {
                    subdirs = dir.GetDirectories();
                }
                catch (System.UnauthorizedAccessException)
                {
                }

                if (subdirs != null)
                {
                    foreach (var d in subdirs)
                    {
                        if (!(_parameters.ExcludeDir?.Invoke(d)?? false))
                        {
                            foreach (var f in TraverseFilesRecursive(d))
                            {
                                yield return f;
                            }
                        }
                    }
                }
            }

            foreach(var dir in _parameters.Dirs)
            {
                foreach (var f in TraverseFilesRecursive(dir))
                {
                    yield return f;
                }
            }
        }
    }
}
