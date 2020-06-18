using System.Collections.Generic;

namespace DeDup.Core
{
    public class DdFileGroup
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

        public long Length => Files[0].FileLength;

        public void AddFile(DdFile f)
        {
            Files.Add(f);
        }

        public IEnumerable<DdFileGroup> Split()
        {
            var files = new List<DdFile>();
            foreach (var f in Files)
            {
                if (f.InitializeRead())
                {
                    files.Add(f);
                }
            }
            if (files.Count >= 2)
            {
                foreach (var ll in SplitRecursive(files, Length))
                {
                    System.Diagnostics.Debug.Assert(ll.Count > 1);
                    var r = new DdFileGroup(ll);
                    System.Diagnostics.Debug.Assert(r.Files.Count > 1);
                    yield return r;
                }
            }
        }

        private static IEnumerable<List<DdFile>> SplitRecursive(List<DdFile> group, long remaining)
        {
            System.Diagnostics.Debug.Assert(group.Count > 1);
            while(remaining-- > 0)
            {
                var b0 = group[0].ReadByteAffirmative();
                for (var j = 1; j < group.Count; j++)
                {
                    var bj = group[j].ReadByteAffirmative();
                    if (bj != b0)
                    {
                        // 0 to j-1 is a group
                        var dict = new Dictionary<byte, List<DdFile>>();
                        dict[b0] = group.GetRange(0, j);
                        dict[bj] = new List<DdFile>{group[j]};

                        for (j++ ; j < group.Count; j++)
                        {
                            var b = group[j].ReadByteAffirmative();
                            if (!dict.TryGetValue(b, out var lf))
                            {
                                lf = new List<DdFile>();
                            }
                            lf.Add(group[j]);
                        }
                        foreach(var ll in dict.Values)
                        {
                            if (ll.Count > 1)
                            {
                                var sr = SplitRecursive(ll, remaining);
                                foreach (var s in sr)
                                {
                                    yield return s;
                                }
                            }
                            else
                            {
                                ll[0].FinalizeRead();
                            }
                        }
                        yield break;
                    }
                }
            }
            yield return group;
        }
    }
}
