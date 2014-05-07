using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Hiale.NgenGui.FolderBrowserDialog.Interop;

namespace Hiale.NgenGui.FolderBrowserDialog
{
    internal static class NativeMethods
    {
        private const string Shell32 = "shell32.dll";
        private const string User32 = "user32.dll";

        public const int ErrorFileNotFound = 2;

        public static bool IsWindowsVistaOrLater
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                       Environment.OSVersion.Version >= new Version(6, 0, 6000);
            }
        }

        [DllImport(User32, CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetActiveWindow();

        #region File Operations Definitions

        #region Nested type: CDCONTROLSTATE

        internal enum CDCONTROLSTATE
        {
            CDCS_INACTIVE = 0x00000000,
            CDCS_ENABLED = 0x00000001,
            CDCS_VISIBLE = 0x00000002
        }

        #endregion

        #region Nested type: COMDLG_FILTERSPEC

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        internal struct COMDLG_FILTERSPEC
        {
            [MarshalAs(UnmanagedType.LPWStr)] internal string pszName;
            [MarshalAs(UnmanagedType.LPWStr)] internal string pszSpec;
        }

        #endregion

        #region Nested type: FDAP

        internal enum FDAP
        {
            FDAP_BOTTOM = 0x00000000,
            FDAP_TOP = 0x00000001,
        }

        #endregion

        #region Nested type: FDE_OVERWRITE_RESPONSE

        internal enum FDE_OVERWRITE_RESPONSE
        {
            FDEOR_DEFAULT = 0x00000000,
            FDEOR_ACCEPT = 0x00000001,
            FDEOR_REFUSE = 0x00000002
        }

        #endregion

        #region Nested type: FDE_SHAREVIOLATION_RESPONSE

        internal enum FDE_SHAREVIOLATION_RESPONSE
        {
            FDESVR_DEFAULT = 0x00000000,
            FDESVR_ACCEPT = 0x00000001,
            FDESVR_REFUSE = 0x00000002
        }

        #endregion

        #region Nested type: FOS

        [Flags]
        internal enum FOS : uint
        {
            FOS_OVERWRITEPROMPT = 0x00000002,
            FOS_STRICTFILETYPES = 0x00000004,
            FOS_NOCHANGEDIR = 0x00000008,
            FOS_PICKFOLDERS = 0x00000020,
            FOS_FORCEFILESYSTEM = 0x00000040, // Ensure that items returned are filesystem items.
            FOS_ALLNONSTORAGEITEMS = 0x00000080, // Allow choosing items that have no storage.
            FOS_NOVALIDATE = 0x00000100,
            FOS_ALLOWMULTISELECT = 0x00000200,
            FOS_PATHMUSTEXIST = 0x00000800,
            FOS_FILEMUSTEXIST = 0x00001000,
            FOS_CREATEPROMPT = 0x00002000,
            FOS_SHAREAWARE = 0x00004000,
            FOS_NOREADONLYRETURN = 0x00008000,
            FOS_NOTESTFILECREATE = 0x00010000,
            FOS_HIDEMRUPLACES = 0x00020000,
            FOS_HIDEPINNEDPLACES = 0x00040000,
            FOS_NODEREFERENCELINKS = 0x00100000,
            FOS_DONTADDTORECENT = 0x02000000,
            FOS_FORCESHOWHIDDEN = 0x10000000,
            FOS_DEFAULTNOMINIMODE = 0x20000000
        }

        #endregion

        #region Nested type: SIATTRIBFLAGS

        internal enum SIATTRIBFLAGS
        {
            SIATTRIBFLAGS_AND = 0x00000001, // if multiple items and the attirbutes together.
            SIATTRIBFLAGS_OR = 0x00000002, // if multiple items or the attributes together.
            SIATTRIBFLAGS_APPCOMPAT = 0x00000003,
            // Call GetAttributes directly on the ShellFolder for multiple attributes
        }

        #endregion

        #region Nested type: SIGDN

        internal enum SIGDN : uint
        {
            SIGDN_NORMALDISPLAY = 0x00000000, // SHGDN_NORMAL
            SIGDN_PARENTRELATIVEPARSING = 0x80018001, // SHGDN_INFOLDER | SHGDN_FORPARSING
            SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000, // SHGDN_FORPARSING
            SIGDN_PARENTRELATIVEEDITING = 0x80031001, // SHGDN_INFOLDER | SHGDN_FOREDITING
            SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000, // SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
            SIGDN_FILESYSPATH = 0x80058000, // SHGDN_FORPARSING
            SIGDN_URL = 0x80068000, // SHGDN_FORPARSING
            SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001, // SHGDN_INFOLDER | SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
            SIGDN_PARENTRELATIVE = 0x80080001 // SHGDN_INFOLDER
        }

        #endregion

        #endregion

        // Property System structs and consts

        #region Nested type: PROPERTYKEY

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct PROPERTYKEY
        {
            internal Guid fmtid;
            internal uint pid;
        }

        #endregion

        #region Shell Parsing Names

        [DllImport(Shell32, CharSet = CharSet.Unicode)]
        public static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

        public static IShellItem CreateItemFromParsingName(string path)
        {
            object item;
            var guid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"); // IID_IShellItem
            int hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref guid, out item);
            if (hr != 0)
                throw new Win32Exception(hr);
            return (IShellItem) item;
        }

        #endregion

        #region Downlevel folder browser dialog

        #region Delegates

        public delegate int BrowseCallbackProc(IntPtr hwnd, FolderBrowserDialogMessage msg, IntPtr lParam, IntPtr wParam);

        #endregion

        #region BrowseInfoFlags enum

        [Flags]
        public enum BrowseInfoFlags
        {
            ReturnOnlyFsDirs = 0x00000001,
            DontGoBelowDomain = 0x00000002,
            StatusText = 0x00000004,
            ReturnFsAncestors = 0x00000008,
            EditBox = 0x00000010,
            Validate = 0x00000020,
            NewDialogStyle = 0x00000040,
            UseNewUI = NewDialogStyle | EditBox,
            BrowseIncludeUrls = 0x00000080,
            UaHint = 0x00000100,
            NoNewFolderButton = 0x00000200,
            NoTranslateTargets = 0x00000400,
            BrowseForComputer = 0x00001000,
            BrowseForPrinter = 0x00002000,
            BrowseIncludeFiles = 0x00004000,
            Shareable = 0x00008000,
            BrowseFileJunctions = 0x00010000
        }

        #endregion

        #region FolderBrowserDialogMessage enum

        public enum FolderBrowserDialogMessage
        {
            Initialized = 1,
            SelChanged = 2,
            ValidateFailedA = 3,
            ValidateFailedW = 4,
            EnableOk = 0x465,
            SetSelection = 0x467
        }

        #endregion

        [DllImport(Shell32, CharSet = CharSet.Unicode)]
        public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

        [DllImport(Shell32, SetLastError = true)]
        public static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, Environment.SpecialFolder nFolder, ref IntPtr ppidl);

        [DllImport(Shell32, PreserveSig = false)]
        public static extern IMalloc SHGetMalloc();

        [DllImport(Shell32, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

        [DllImport(User32, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, FolderBrowserDialogMessage msg, IntPtr wParam, string lParam);

        [DllImport(User32)]
        public static extern IntPtr SendMessage(IntPtr hWnd, FolderBrowserDialogMessage msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public string pszDisplayName;
            [MarshalAs(UnmanagedType.LPTStr)] public string lpszTitle;
            public BrowseInfoFlags ulFlags;
            public BrowseCallbackProc lpfn;
            public IntPtr lParam;
            public int iImage;
        }

        #endregion

        //#region String Resources

        //[Flags()]
        //public enum FormatMessageFlags
        //{
        //    FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
        //    FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
        //    FORMAT_MESSAGE_FROM_STRING = 0x00000400,
        //    FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
        //    FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
        //    FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000
        //}

        ////[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        ////public static extern int LoadString(SafeModuleHandle hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        //[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        //public static extern uint FormatMessage([MarshalAs(UnmanagedType.U4)] FormatMessageFlags dwFlags, IntPtr lpSource,
        //   uint dwMessageId, uint dwLanguageId, ref IntPtr lpBuffer,
        //   uint nSize, string[] Arguments);

        //#endregion
    }
}