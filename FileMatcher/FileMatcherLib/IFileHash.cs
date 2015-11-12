using System.IO;

namespace FileMatcher
{
    public interface IFileHash
    {
        #region Methods

        /// <summary>
        ///  generates a key for the specified file
        /// </summary>
        /// <param name="fileInfo">The file to generate a key for</param>
        /// <returns>the key generated for the file</returns>
        int Hash(FileInfo fileInfo);

        #endregion
    }
}
