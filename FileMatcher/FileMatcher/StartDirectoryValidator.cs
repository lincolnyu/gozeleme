using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileMatcher
{
    /// <summary>
    ///  A class that removes redudant sub-directories from a collection of directories
    /// </summary>
    public static class StartDirectoryValidator
    {
        #region Nested types

        /// <summary>
        ///  compares paths in alphabetic order
        /// </summary>
        public class PathComparer : IComparer<DirectoryInfo>
        {
            #region Fields

            /// <summary>
            ///  the singleton
            /// </summary>
            public static readonly PathComparer Instance = new PathComparer();

            #endregion

            /// <summary>
            ///  compares two directories by the name
            /// </summary>
            /// <param name="x">The first directory to compare</param>
            /// <param name="y">The second directory to compare</param>
            /// <returns>The comparison result</returns>
            public int Compare(DirectoryInfo x, DirectoryInfo y)
            {
                return string.Compare(x.FullName, y.FullName, StringComparison.Ordinal);
            }
        }

        #endregion

        #region Methods

        public static void Validate(IEnumerable<DirectoryInfo> startingDirs, 
            IEnumerable<DirectoryInfo> excludedDirs, out List<DirectoryInfo> validatedStartingDirs,
            out ISet<DirectoryInfo> validatedExcludedDirs)
        {
            validatedStartingDirs = Validate(startingDirs);
            if (excludedDirs != null)
            {
                validatedExcludedDirs = new HashSet<DirectoryInfo>();
                foreach (var excludedDir in excludedDirs)
                {
                    var isValidExcludedDir = false;
                    foreach (var vsd in validatedStartingDirs)
                    {
                        if (vsd.FullName.IsStrictSubStringIgnoreCase(excludedDir.FullName))
                        {
                            isValidExcludedDir = true;
                            break;
                        }
                    }
                    if (isValidExcludedDir)
                    {
                        validatedExcludedDirs.Add(excludedDir);
                    }
                }
            }
            else
            {
                validatedExcludedDirs = null;
            }
        }

        /// <summary>
        ///  returns a list of directories that don't overlap and cover all the input
        /// </summary>
        /// <param name="startingDirs">The input starting directories</param>
        /// <returns>The non-redundant starting directories</returns>
        public static List<DirectoryInfo> Validate(IEnumerable<DirectoryInfo> startingDirs)
        {
            var result = new List<DirectoryInfo>();

            foreach (var sp in startingDirs)
            {
                var pos = result.BinarySearch(sp, PathComparer.Instance);
                if (pos >= 0)
                {
                    continue;
                }
                pos = -pos - 1;

                if (pos < result.Count && sp.FullName.IsStrictSubStringIgnoreCase(result[pos].FullName))
                {   // the one to be inserted is a substring of the existing (ancestral folder)
                    var j = pos;
                    do
                    {
                        j++;
                    } while (j < result.Count && sp.FullName.IsStrictSubStringIgnoreCase(result[j].FullName));
                    result.RemoveRange(pos, j - pos);
                    result.Insert(pos, sp);
                }
                else if (!(pos > 0 && result[pos - 1].FullName.IsStrictSubStringIgnoreCase(sp.FullName)))
                {
                    result.Insert(pos, sp);
                }
                // otherwise the one to be inserted is a subfolder
            }

            return result;
        }

        /// <summary>
        ///  Determines if the specified string is the subtring of the other
        /// </summary>
        /// <param name="candidateSubstr">The candid substring</param>
        /// <param name="superStr">The candid super string</param>
        /// <returns>True if it is a substring</returns>
        public static bool IsStrictSubStringIgnoreCase(this string candidateSubstr, string superStr)
        {
            if (candidateSubstr.Length >= superStr.Length)
            {
                return false;
            }
            return string.Compare(candidateSubstr, superStr.Substring(0, candidateSubstr.Length),
                StringComparison.OrdinalIgnoreCase) == 0;
        }

        internal static void Validate(IEnumerable<DirectoryInfo> startingDirs, IEnumerable<DirectoryInfo> excludedDirs, out object validatedStartingDirs, out object validatedExcludedDirs)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
