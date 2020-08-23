using System.IO;
using System.Collections.Generic;

namespace CcDupResolver
{
    class DupFile
    {
        public enum FileTypes
        {
            Unprocessed,
            Master,
            Slave
        }

        public DupFile()
        {
        }

        public DupFile(string filePath)
        {
            Dir = Path.GetDirectoryName(filePath);
            FileName = Path.GetFileName(filePath);
        }

        public FileTypes FileType = FileTypes.Unprocessed;
        public string Dir;
        public string FileName;
        public string FullPath => Path.Combine(Dir, FileName);
        public long FileLength => new FileInfo(FullPath).Length;
        public string CcDupFullPath => ToCcDup(FullPath);

        public DupFile Master = null;
        public List<DupFile> Slaves = new List<DupFile>();

        public static string ToCcDup(string s) => s + ".ccdup";

        public IEnumerable<string> DeserializeToStrings()
        {
            System.Diagnostics.Debug.Assert(File.Exists(CcDupFullPath));
            using var ccdupr = new StreamReader(CcDupFullPath);
            while (!ccdupr.EndOfStream)
            {
                var origDup = ccdupr.ReadLine();
                if (string.IsNullOrWhiteSpace(origDup))
                {
                    continue;
                }
                if (!Path.IsPathRooted(origDup))
                {
                    origDup = FileHelper.ToAbsolute(origDup, new DirectoryInfo(Dir)).FullName;
                }

                yield return origDup;
            }
        }

        public void Serialize()
        {
            switch (FileType)
            {
                case FileTypes.Master:
                    SerializeMaster();
                    break;
                case FileTypes.Slave:
                    SerializeSlave();
                    File.Delete(FileName);
                    break;
                case FileTypes.Unprocessed:
                    if (File.Exists(CcDupFullPath))
                    {
                        File.Delete(CcDupFullPath);
                    }
                    break;
            }
        }

        private void SerializeMaster()
        {
            using var ccdupw = new StreamWriter(CcDupFullPath);
            foreach (var slave in Slaves)
            {
                var slavePath = FileHelper.ToRelative(new FileInfo(slave.FullPath), new DirectoryInfo(Dir));
                ccdupw.WriteLine(slavePath);
            }
        }

        private void SerializeSlave()
        {
            var masterPath = FileHelper.ToRelative(new FileInfo(Master.FullPath), new DirectoryInfo(Dir));
            using var ccdupw = new StreamWriter(CcDupFullPath);
            ccdupw.WriteLine(masterPath);
        }
    }
}

