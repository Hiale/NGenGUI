using System.Runtime.InteropServices;
using Hiale.NgenGui.FolderBrowserDialog.Interop;

namespace Ookii.Dialogs.Wpf.Interop
{
    //// Dummy base interface for CommonFileDialog coclasses
    //internal interface NativeCommonFileDialog
    //{
    //}

    // ---------------------------------------------------------
    // Coclass interfaces - designed to "look like" the object
    // in the API, so that the 'new' operator can be used in a
    // straightforward way. Behind the scenes, the C# compiler
    // morphs all 'new CoClass()' calls to 'new CoClassWrapper()'
    [ComImport,
     Guid(IIDGuid.IFileOpenDialog),
     CoClass(typeof (FileOpenDialogRCW))]
    internal interface NativeFileOpenDialog : IFileOpenDialog
    {
    }

    // ---------------------------------------------------
    // .NET classes representing runtime callable wrappers
    [ComImport,
     ClassInterface(ClassInterfaceType.None),
     TypeLibType(TypeLibTypeFlags.FCanCreate),
     Guid(CLSIDGuid.FileOpenDialog)]
    internal class FileOpenDialogRCW
    {
    }
}