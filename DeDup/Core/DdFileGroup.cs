using System;
using System.Collections.Generic;
using static DeDup.Core.SplitHeler;
using CcDupList;

namespace DeDup.Core
{
    public class DdFileGroup : IDupFileGroup<DdFile>
    {
        public DdFileGroup()
        {
        }

        public DdFileGroup(IEnumerable<DdFile> files)
        {
            foreach (var f in files)
            {
                AddFile(f);
            }
        }

        public List<DdFile> Files { get; } = new List<DdFile>();

        ICollection<DdFile> IDupFileGroup<DdFile>.Files => Files;

        public long FileLength => Files[0].FileLength;

        public void AddFile(DdFile f)
        {
            Files.Add(f);
        }

        public IEnumerable<DdFileGroup> Split(Action<DdFile> addFailedFile)
        {
            var files = new List<DdFile>();
            foreach (var f in Files)
            {
                if (f.InitializeRead())
                {
                    files.Add(f);
                }
                else
                {
                    addFailedFile(f);
                }
            }
            if (files.Count >= 2)
            {
                foreach (var ll in SplitRecursive<DdFile>(files, FileLength, addFailedFile))
                {
                    yield return new DdFileGroup(ll);
                }
            }
        }
    }
}
