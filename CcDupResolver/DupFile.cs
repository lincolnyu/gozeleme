using System.IO;

namespace CcDupResolver
{
    class DupFile
    {
        public string Dir;
        public string File;
        public string FullPath => Path.Combine(Dir, File);
        public long FileLength => new FileInfo(FullPath).Length;
        public string CcDupFullPath => ToCcDup(FullPath);

        public static string ToCcDup(string s) => s + ".ccdup";
    }
}

