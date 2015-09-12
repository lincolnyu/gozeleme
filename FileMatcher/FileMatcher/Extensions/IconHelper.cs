using System;
using System.Drawing;

namespace FileMatcher.Extensions
{
    public static class IconHelper
    {
        public static Icon GetIcon(string iconFile, int iconIndex)
        {
            var hIconEx = new[] {IntPtr.Zero};
            var iconCount = Win32Api.ExtractIconEx(
                iconFile,
                iconIndex,
                null,
                hIconEx,
                1);

            if (iconCount == 0) return null;

            // If success then return as a GDI+ object
            Icon icon = null;
            if (hIconEx[0] != IntPtr.Zero)
            {
                icon = Icon.FromHandle(hIconEx[0]);
            }
            return icon;
        }
    }
}
