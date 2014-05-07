using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Hiale.NgenGui.Helper
{
    public class ShellIcon
    {
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        private const uint SHGFI_ICON = 0x000000100; // get icon
        private const uint SHGFI_SMALLICON = 0x000000001; // get small icon
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010; // use passed dwFileAttribute

        public static ImageSource GetIcon(string path)
        {
            return GetIcon(path, false, false);
        }

        public static ImageSource GetIcon(string path, bool smallIcon, bool isDirectory)
        {
            // SHGFI_USEFILEATTRIBUTES takes the file name and attributes into account if it doesn't exist
            var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;
            if (smallIcon)
                flags |= SHGFI_SMALLICON;

            var attributes = FILE_ATTRIBUTE_NORMAL;
            if (isDirectory)
                attributes |= FILE_ATTRIBUTE_DIRECTORY;

            SHFILEINFO shfi;
            return SHGetFileInfo(path, attributes, out shfi, (uint) Marshal.SizeOf(typeof (SHFILEINFO)), flags) != 0 ? Imaging.CreateBitmapSourceFromHIcon(shfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()) : null;
        }

        [DllImport("shell32.dll")]
        private static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint flags);

        #region Nested type: SHFILEINFO

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public readonly IntPtr hIcon;
            private readonly int iIcon;
            private readonly uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            private readonly string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            private readonly string szTypeName;
        }

        #endregion
    }
}