using System;
using System.Runtime.InteropServices;

namespace FileMatcherApp.Extensions
{
    public static class Win32Api
    {
        [DllImport("Shell32", CharSet = CharSet.Auto)]
        internal static extern int ExtractIconEx(
            [MarshalAs(UnmanagedType.LPTStr)] string lpszFile,
            int nIconIndex,
            IntPtr[] phIconLarge,
            IntPtr[] phIconSmall,
            int nIcons);
    }
}
