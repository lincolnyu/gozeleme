using System.IO;

namespace CcDupResolver
{
    class CcdupMove
    {
        public DirectoryInfo SourceFolder {get;}
        public DirectoryInfo TargetFolder {get;}
 
        public CcdupMove(DirectoryInfo sourceFolder, DirectoryInfo targetFolder)
        {
            SourceFolder = sourceFolder;
            TargetFolder = targetFolder;
        }

        public FileInfo Rename(FileInfo originalFile)
        {
            if (!originalFile.FullName.StartsWith(SourceFolder.FullName))
            {
                return null;
            }
            return new FileInfo(Path.Combine(TargetFolder.FullName, 
                originalFile.FullName.Substring(SourceFolder.FullName.Length)));
        }
    }
}