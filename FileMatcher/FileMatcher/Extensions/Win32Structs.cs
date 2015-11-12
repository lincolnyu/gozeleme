using System.Runtime.InteropServices;

namespace FileMatcherApp.Extensions
{
    public static class Win32Structs
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Unicode)]
        // ReSharper disable InconsistentNaming
        public struct _WIN32_FIND_DATAW
            // ReSharper restore InconsistentNaming
        {
            public uint dwFileAttributes;
            public _FILETIME ftCreationTime;
            public _FILETIME ftLastAccessTime;
            public _FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0)]
        // ReSharper disable InconsistentNaming
        public struct _FILETIME
            // ReSharper restore InconsistentNaming
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }
    }
}
