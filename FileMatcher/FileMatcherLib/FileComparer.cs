using System;
using System.Collections.Generic;
using System.IO;

namespace FileMatcherLib
{
    /// <summary>
    ///  Compares identical file groups first by their length and then their binary contents in dictionary order
    /// </summary>
    public class FileComparer : IComparer<IdenticalFiles>
    {
        #region Constructors

        /// <summary>
        ///  Instantiates the singleton
        /// </summary>
        static FileComparer()
        {
            Instance = new FileComparer();
        }

        #endregion

        #region Properties

        /// <summary>
        ///  The singleton of this type
        /// </summary>
        public static FileComparer Instance { get; private set; }

        #endregion

        #region Methods

        #region IComparer<FileInfo> members

        /// <summary>
        ///  compares two identical file group first by their length and then their binary contents
        ///  in dictionary order
        /// </summary>
        /// <param name="x">The first identical file group to compare</param>
        /// <param name="y">The second identical file group file to compare</param>
        /// <returns>An integer that indicates the comparison result</returns>
        public int Compare(IdenticalFiles x, IdenticalFiles y)
        {
            var f1 = x.First.Value;
            var f2 = y.First.Value;
            if (f1.Length < f2.Length)
            {
                return -1;
            }
            if (f1.Length > f2.Length)
            {
                return 1;
            }
            return CompareBinaryContents(f1, f2, f1.Length);
        }

        #endregion

        /// <summary>
        ///  compares two files by their binary contents up to specified byte length
        /// </summary>
        /// <param name="f1">The first file to compare</param>
        /// <param name="f2">The second file to compare</param>
        /// <param name="length">The length to compare within</param>
        /// <returns>An integer that indicates the comparison result</returns>
        public static int CompareBinaryContents(FileInfo f1, FileInfo f2, long length)
        {
            var fs1 = f1.OpenRead();
            var fs2 = f2.OpenRead();
            const long lenThr = 64 * 1024 * 1024;
            const int smallBuf = 1024 * 1024;
            const int largeBuf = 16 * 1024 * 1024;
            var bufSize = length > lenThr ? largeBuf : smallBuf;
            var buf1 = new byte[bufSize];
            var buf2 = new byte[bufSize];

            for (var totalRead = 0; totalRead < length; )
            {
                var read1 = fs1.Read(buf1, 0, bufSize);
                var read2 = fs2.Read(buf2, 0, bufSize);
                if (read1 != read2)
                {
                    throw new Exception("Unexpected discrepancy in file length");
                }
                var read = read1 < read2 ? read1 : read2;
                totalRead += read;
                if (read == 0)
                {
                    // NOTE it is possible that some system files can change their sizes
                    return read1.CompareTo(read2);
                }
                for (var i = 0; i < read; i++)
                {
                    var b1 = buf1[i];
                    var b2 = buf2[i];
                    if (b1 == b2)
                    {
                        continue;
                    }
                    if (b1 < b2)
                    {
                        return -1;
                    }
                    if (b1 > b2)
                    {
                        return 1;
                    }
                }
            }
            return 0;
        }

        #endregion
    }
}
