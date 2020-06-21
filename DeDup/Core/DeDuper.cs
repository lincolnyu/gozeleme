using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Logging;

namespace DeDup.Core
{
    public class DeDuper
    {
        public class Parameters
        {
             public IEnumerable<DirectoryInfo> Dirs { get; set; }
            public Predicate<DirectoryInfo> ExcludeDir { get; set; }
            public Predicate<FileInfo> IncludeFile { get; set; }
            public Predicate<FileInfo> ExcludeFile { get; set; }
            public ParallelOptions ParallelOptions { get; set; }
            public Logger Logger { get; set; }
        }

        private DeDuper.Parameters _parameters;

        public DeDuper(DeDuper.Parameters parameters)
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
                _parameters.Logger?.Info($"\r{++fileCount} files added...");
            }
            var t1 = DateTime.UtcNow;
            _parameters.Logger?.InfoLine($"\r{fileCount} files added, {(t1-t0).TotalSeconds:0.00} seconds taken.");

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
                            _parameters.Logger?.Info($"\r{++groupCount}/{dict.Count} groups split. File length of last group: {dfg.FileLength.StringifyFileLength()}   ");
                        }
                    });
            }
            else
            {
                foreach (var dfg in dict.Values.Reverse())
                {
                    DupFileGroups.AddRange(dfg.Split(ff=>FailedFiles.Add(ff)));
                    _parameters.Logger?.Info($"\r{++groupCount}/{dict.Count} groups split...");
                }
            }
            var t2 = DateTime.UtcNow;
            _parameters.Logger?.InfoLine($"\r{dict.Count} groups split into {DupFileGroups.Count}, {(t2-t1).TotalSeconds:0.00} seconds taken.");

            _parameters.Logger?.Info($"\rSorting groups...");
            DupFileGroups.Sort((a,b)=>b.FileLength.CompareTo(a.FileLength));
            var t3 = DateTime.UtcNow;
            _parameters.Logger?.InfoLine($"\rGroups sorted, {(t3-t2).TotalSeconds:0.00} seconds taken.");
            _parameters.Logger?.InfoLine($"\rDeDup all done, {(t3-t0).TotalSeconds:0.00} seconds taken.");
        }
        public List<DdFileGroup> DupFileGroups { get; } = new List<DdFileGroup>();
        public List<DdFile> FailedFiles { get; } = new List<DdFile>();
        
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
