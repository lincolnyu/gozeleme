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
            var t0 = DateTime.UtcNow;
            var dict = new SortedDictionary<long, DdFileGroup>();
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
            var t1 = DateTime.UtcNow;
            InfoLine($"\r{fileCount} files added, took {(t1-t0).TotalSeconds:0.00} seconds.");

            var groupCount = 0;
            //TODO cancelaltion
            if (_parameters.ParallelOptions != null)
            {
                Parallel.ForEach(dict.Values.Reverse(),
                    _parameters.ParallelOptions, 
                    dfg=>
                    {
                        foreach (var d in dfg.Split(ff=>{ lock(this) {FailedFiles.Add(ff);} }))
                        {
                            lock(this)
                            {
                                DupFileGroups.Add(d);
                            }
                        }
                        lock(this)
                        {
                            Info($"\r{++groupCount}/{dict.Count} groups split...");
                        }
                    });
            }
            else
            {
                foreach (var dfg in dict.Values.Reverse())
                {
                    DupFileGroups.AddRange(dfg.Split(ff=>FailedFiles.Add(ff)));
                    Info($"\r{++groupCount}/{dict.Count} groups split...");
                }
            }
            var t2 = DateTime.UtcNow;
            InfoLine($"\r{dict.Count} groups split into {DupFileGroups.Count}, took {(t2-t1).TotalSeconds:0.00} seconds.");

            Info($"\rSorting groups...");
            DupFileGroups.Sort((a,b)=>b.Length.CompareTo(a.Length));
            var t3 = DateTime.UtcNow;
            InfoLine($"\rGroups sorted, took {(t3-t2).TotalSeconds:0.00} seconds.");
            InfoLine($"\rDeDup done, totally took {(t3-t0).TotalSeconds:0.00} seconds.");
        }
        public List<DdFileGroup> DupFileGroups { get; } = new List<DdFileGroup>();
        public List<DdFile> FailedFiles { get; } = new List<DdFile>();
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
