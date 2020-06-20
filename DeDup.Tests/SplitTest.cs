using DeDup.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static DeDup.Core.SplitHeler;

namespace DeDup.Tests
{
    public class SplitTest
    {
        class MockFile : IReadByteAffirmative
        {
            public enum States
            {
                Uninitialized,
                Initialized,
                Finalized
            }
            public States State = States.Uninitialized;
            public byte[] Bytes;
            public int Pos = 0;
            public bool UnableToInitialize = false;
            public int? FailedPos = null;
            public int FileLength => Bytes.Length;
            public MockFile(params byte[] bytes)
            {
                Bytes = bytes;
            }
            public bool InitializeRead()
            {
                if (UnableToInitialize) return false;
                State = States.Initialized;
                return true;
            }
            public byte ReadByteAffirmative()
            {
                if (State != States.Initialized)
                {
                    throw new IOException("File not initialized");
                }
                if (Pos >= FileLength)
                {
                    throw new IOException("End of stream");
                }
                if (FailedPos.HasValue && Pos >= FailedPos.Value)
                {
                    throw new IOException("Failed to read");
                }
                return Bytes[Pos++];
            }
            public void FinalizeRead()
            {
                State = States.Finalized;
            }
        }

        class MockFileGroup
        {
            public List<MockFile> Files = new List<MockFile>();
            public int Length => Files[0].FileLength;
            public IEnumerable<MockFileGroup> Split(Action<MockFile> addFailedFile)
            {
                var files = new List<MockFile>();
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
                    foreach (var ll in SplitRecursive<MockFile>(files, Length, addFailedFile))
                    {
                        yield return new MockFileGroup{Files = ll};
                    }
                }
            }
        }

        class MockFileGroupSplitter
        {
            public List<MockFile> FailedFiles { get; } = new List<MockFile>();
            SortedDictionary<long, MockFileGroup> _dict = new SortedDictionary<long, MockFileGroup>();
            public void CollectFiles(IEnumerable<MockFile> files)
            {
                foreach(var f in files)
                {
                    if (!_dict.TryGetValue(f.FileLength, out var v))
                    {
                        v = new MockFileGroup();
                        _dict[f.FileLength] = v;
                    }
                    v.Files.Add(f);
                }
            }
            public List<MockFileGroup> ParallelSplit()
            {
                var groups = new List<MockFileGroup>();
                Parallel.ForEach(_dict.Values.Reverse(),
                new ParallelOptions{ MaxDegreeOfParallelism =Environment.ProcessorCount }, 
                dfg=>
                {
                    foreach (var d in dfg.Split(ff=>{ lock(this) {FailedFiles.Add(ff);} }))
                    {
                        lock(this)
                        {
                            groups.Add(d);
                        }
                    }
                });
                return groups;
            }
        }

        [Fact]
        public void TestFailedRead()
        {
            var files = new []
            {
                new MockFile
                {
                    Bytes = new byte[]{1,2,3}
                },
                new MockFile
                {
                    Bytes = new byte[]{1,2,3},
                    FailedPos = 0,
                },
                new MockFile
                {
                    Bytes = new byte[]{1,2,3},
                    UnableToInitialize = true,
                },
                new MockFile
                {
                    Bytes = new byte[]{1,2,3},
                    FailedPos = 1,
                },
                new MockFile
                {
                    Bytes = new byte[]{1,2,3},
                    FailedPos = 1,
                },
                new MockFile
                {
                    Bytes = new byte[]{1,2,4},
                },  
                new MockFile
                {
                    Bytes = new byte[]{1,2,3}
                }
            };
            var splitter = new MockFileGroupSplitter();
            splitter.CollectFiles(files);
            var groups = splitter.ParallelSplit();
            Assert.True(1 == groups.Count);
            Assert.True(2 == groups[0].Files.Count);
        }
    }
}
