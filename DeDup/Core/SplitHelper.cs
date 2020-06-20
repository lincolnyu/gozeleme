using System;
using System.Collections.Generic;
using System.IO;

namespace DeDup.Core
{
    public static class SplitHeler
    {
        public static IEnumerable<List<T>> SplitRecursive<T>(List<T> group, long remaining,
            Action<T> addFailedFile) where T : IReadByteAffirmative
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
                        var dict = new Dictionary<byte, List<T>>();
                        dict[bLast] = group.GetRange(j+1, group.Count-j-1);
                        dict[bj] = new List<T>{group[j]};

                        while (--j >= 0)
                        {
                            if (TryReadByte(group[j], out var b))
                            {
                                if (!dict.TryGetValue(b, out var lf))
                                {
                                    lf = new List<T>();
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
                                var sr = SplitRecursive<T>(ll, remaining, addFailedFile);
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

        private static bool TryReadByte<T>(T f, out byte b) where T : IReadByteAffirmative
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
    }
}
