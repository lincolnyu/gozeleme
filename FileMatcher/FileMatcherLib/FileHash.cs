using System.IO;

namespace FileMatcherLib
{
    /// <summary>
    ///  a class that generates a key for a file
    /// </summary>
    class FileHash : IFileHash
    {
        #region Constructors

        /// <summary>
        ///  creates a singleton
        /// </summary>
        static FileHash()
        {
            Instance = new FileHash();
        }
        
        #endregion

        #region Properties

        /// <summary>
        ///  Returns the singleton
        /// </summary>
        public static FileHash Instance { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        ///  generates a key for the specified file
        /// </summary>
        /// <param name="fileInfo">The file to generate a key for</param>
        /// <returns>the key generated for the file</returns>
        public int Hash(FileInfo fileInfo)
        {
            var len = fileInfo.Length;
            var val = (uint)len;
            if (len > 0)
            {
                var halfLen = len/2;
                using (var fs = fileInfo.OpenRead())
                {
                    fs.Seek(halfLen, SeekOrigin.Begin);
                    var b = (byte)fs.ReadByte();
                    val <<= 8;
                    val |= b;
                }
            }
            return (int)val;
        }

        #endregion
    }
}
