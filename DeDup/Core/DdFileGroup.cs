using System;
using System.Collections.Generic;
using System.IO;

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
                foreach (var ll in SplitRecursive(files, Length, addFailedFile))
                {
                    yield return new DdFileGroup(ll);
                }
            }
        }

        private static bool TryReadByte(DdFile f, out byte b)
        {
            try
            {
                b = f.ReadByteAffirmative();
                return true;
            }
            catch (IOException)
            {
                b = default;
                return false;
            }
        }

        private static IEnumerable<List<DdFile>> SplitRecursive(List<DdFile> group, long remaining, Action<DdFile> addFailedFile)
        {
        _next:
            while (remaining-- > 0)
            {
                System.Diagnostics.Debug.Assert(group.Count > 1);
                byte bLast;
                var j = group.Count-1;
                for (; !TryReadByte(group[j], out bLast); j--)
                {
                    group[j].FinalizeRead();
                    addFailedFile(group[j]);
                    group.RemoveAt(j);
                    if (j == 1)
                    {
                        System.Diagnostics.Debug.Assert(group.Count == 1);
                        goto _final;
                    }
                }
                System.Diagnostics.Debug.Assert(j > 0);
                while (--j >= 0)
                {
                    byte bj;
                    for( ; !TryReadByte(group[j], out bj); j--)
                    {
                        group[j].FinalizeRead();
                        addFailedFile(group[j]);
                        group.RemoveAt(j);
                        if (j == 0)
                        {
                            if (group.Count == 1)
                            {
                                goto _final;
                            }
                            goto _next;
                        }
                    }
                    
                    if (bj != bLast)
                    {
                        // 'last' down to j+1 is a group
                        var dict = new Dictionary<byte, List<DdFile>>();
                        dict[bLast] = group.GetRange(j+1, group.Count-j-1);
                        dict[bj] = new List<DdFile>{group[j]};

                        while (--j >= 0)
                        {
                            if (TryReadByte(group[j], out var b))
                            {
                                if (!dict.TryGetValue(b, out var lf))
                                {
                                    lf = new List<DdFile>();
                                }
                                lf.Add(group[j]);
                            }
                            else
                            {
                                group[j].FinalizeRead();
                                addFailedFile(group[j]);
                            }
                        }
                        foreach(var ll in dict.Values)
                        {
                            if (ll.Count > 1)
                            {
                                var sr = SplitRecursive(ll, remaining, addFailedFile);
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
        _final:
            foreach (var f in group)
            {
                f.FinalizeRead();
            }
            if (group.Count > 1)    // Note group members may be removed so need to check again
            {
                yield return group;
            }
        }
    }
}
