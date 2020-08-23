using Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CcDupResolver
{
    class DupFileGroup
    {
        public readonly SortedDictionary<string, DupFile> Files = new SortedDictionary<string, DupFile>();

        public int Count => Files.Count;

        public bool IsValidDupGroup => Count > 1;

        public DupFile AddFile(string fullName, Logger logger)
        {
            var file = new DupFile(fullName);
            var ccExists = File.Exists(file.CcDupFullPath);
            var fileExists = File.Exists(file.FullPath);
            if (ccExists && fileExists)
            {
                file.FileType = DupFile.FileTypes.Master;
                var origDups = file.DeserializeToStrings();
                foreach (var origDup in origDups)
                {
                    var origCcDup = DupFile.ToCcDup(origDup);
                    if (!File.Exists(origCcDup))
                    {
                        // error
                        logger.ErrorLine($"Error: Original placeholder file '{origCcDup}' not found.");
                        continue;
                    }
                    if (Files.TryGetValue(origDup, out var slave))
                    {
                        if (slave.FileType == DupFile.FileTypes.Unprocessed)
                        {
                            slave.FileType = DupFile.FileTypes.Slave;
                        }
                    }
                    else
                    {
                        slave = new DupFile
                        {
                            Dir = Path.GetDirectoryName(origDup),
                            FileName = Path.GetFileName(origDup),
                            FileType = DupFile.FileTypes.Slave
                        };
                        Files[fullName] = slave;
                    }
                    file.Slaves.Add(slave);
                    slave.Master = file;
                }
            }
            else if (fileExists)
            {
                file.FileType = DupFile.FileTypes.Unprocessed;
            }
            else if (ccExists)
            {
                file.FileType = DupFile.FileTypes.Slave;
            }
            logger.ErrorLine($"Error: File '{file.FullPath}' does not exist.");
            return null;
        }

        public void Validate(Logger logger)
        {
            foreach (var f in Files.Values)
            {
                if (f.FileType == DupFile.FileTypes.Slave && f.Master == null)
                {
                    var l = f.DeserializeToStrings().ToArray();
                    if (l.Length == 0)
                    {
                        logger.ErrorLine($"Error: '{f.CcDupFullPath}' empty or bad");
                        f.FileType = DupFile.FileTypes.Unprocessed;
                    }
                    else
                    {
                        if (l.Length > 1)
                        {
                            logger.ErrorLine($"Error: '{f.CcDupFullPath}' empty or bad");
                        }
                        foreach (var p in l)
                        {
                            if (Files.TryGetValue(p, out var fp))
                            {
                                f.Master = fp;
                                break;
                            }
                        }
                        if (f.Master == null)
                        {
                            f.FileType = DupFile.FileTypes.Unprocessed;
                        }
                    }
                    f.Serialize();
                }
            }
        }

        public void Resolve(DupFile selected)
        {
            selected.Slaves.Clear();
            foreach (var f in Files.Values)
            {
                if (f != selected)
                {
                    selected.Slaves.Add(f);
                    f.FileType = DupFile.FileTypes.Slave;
                    selected.Serialize();
                }
            }
            selected.FileType = DupFile.FileTypes.Master;
            selected.Serialize();
        }
    }
}
