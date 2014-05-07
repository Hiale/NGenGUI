using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Hiale.NgenGui
{
   public class TaskbarProgressBar
   {
      [DllImport("user32.dll", CharSet = CharSet.Auto)]
      internal static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

      private int _value;

      private bool _showInTaskbar;
      private ProgressBarState _state = ProgressBarState.Normal;
      private ThumbnailProgressState _style = ThumbnailProgressState.Normal;

      private readonly IntPtr _handle;

      private static volatile ITaskbarList3 _sTaskbarList;
      private static readonly OperatingSystem OSInfo = Environment.OSVersion;

      /// <summary>
      /// Show progress in taskbar
      /// </summary>
      [DefaultValue(false)]
      public bool ShowInTaskbar
      {
         get { return _showInTaskbar; }
         set
         {
            if (_showInTaskbar == value)
               return;
            _showInTaskbar = value;

            // send signal to the taskbar.
            if (_handle == IntPtr.Zero)
               return;
            if (Style != ThumbnailProgressState.Indeterminate)
               SetValue();
            SetState();
         }
      }

      /// <summary>
      /// Gets or sets the current position of the progress bar.
      /// </summary>
      /// <returns>The position within the range of the progress bar. The default is 0.</returns>
      public int Value
      {
         get { return _value; }
         set
         {
            _value = value;

            // send signal to the taskbar.
            SetValue();
         }
      }

      /// <summary>
      /// Gets or sets the manner in which progress should be indicated on the progress bar.
      /// </summary>
      /// <returns>One of the ProgressBarStyle values. The default is ProgressBarStyle.Blocks</returns>
      public ThumbnailProgressState Style
      {
         get { return _style; }
         set
         {
            _style = value;

            // set the style of the progress bar
            if (_showInTaskbar && _handle != IntPtr.Zero)
            {
               SetState();
            }
         }
      }


      /// <summary>
      /// The progress bar state for Windows Vista & 7
      /// </summary>
      [DefaultValue(ProgressBarState.Normal)]
      public ProgressBarState State
      {
         get { return _state; }
         set
         {
            _state = value;
            var wasMarquee = Style == ThumbnailProgressState.Indeterminate;
            if (wasMarquee)
               Style = ThumbnailProgressState.Normal; // sets the state to normal (and implicity calls SetState())

            // set the progress bar state (Normal, Error, Paused)
            SendMessage(_handle, 0x410, (int) value, 0);

            if (wasMarquee)
               // the Taskbar PB value needs to be reset
               SetValue();
            else
               // there wasn't a marquee, thus we need to update the taskbar
               SetState();
         }
      }

      [DefaultValue(0)]
      public int Minimum { get; set; }

      [DefaultValue(100)]
      public int Maximum { get; set; }

      private static ITaskbarList3 TaskbarList
      {
         get
         {
            if (_sTaskbarList == null)
            {
               lock (typeof (TaskbarProgressBar))
               {
                  if (_sTaskbarList == null)
                     _sTaskbarList = (ITaskbarList3) new CTaskbarList();
                  _sTaskbarList.HrInit();
               }
            }
            return _sTaskbarList;
         }
      }

      public TaskbarProgressBar()
      {
         Maximum = 100;
      }

      public TaskbarProgressBar(IntPtr handle) : this()
      {
         _handle = handle;
      }

      private void SetValue()
      {
         if (!_showInTaskbar)
            return;
         var maximum = (ulong) (Maximum - Minimum);
         var progress = (ulong) (Value - Minimum);

         SetProgressValue(_handle, progress, maximum);
      }

      private void SetState()
      {
         if (_handle == IntPtr.Zero)
            return;
         var thmState = !_showInTaskbar ? ThumbnailProgressState.NoProgress : Style;
         SetProgressState(_handle, thmState);
      }

      /// <summary>
      /// Sets the progress state of the specified window's
      /// taskbar button.
      /// </summary>
      /// <param name="hWnd">The window handle.</param>
      /// <param name="state">The progress state.</param>
      public static void SetProgressState(IntPtr hWnd, ThumbnailProgressState state)
      {
         if (Windows7OrGreater)
            TaskbarList.SetProgressState(hWnd, state);
      }

      /// <summary>
      /// Sets the progress value of the specified window's
      /// taskbar button.
      /// </summary>
      /// <param name="hWnd">The window handle.</param>
      /// <param name="current">The current value.</param>
      /// <param name="maximum">The maximum value.</param>
      public static void SetProgressValue(IntPtr hWnd, ulong current, ulong maximum)
      {
         if (Windows7OrGreater)
            TaskbarList.SetProgressValue(hWnd, current, maximum);
      }

      private static bool Windows7OrGreater
      {
         get
         {
            return OSInfo.Platform == PlatformID.Win32NT &&
                   ((OSInfo.Version.Major == 6 && OSInfo.Version.Minor >= 1) || (OSInfo.Version.Major > 6));
         }
      }

      [ComImport]
      [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
      [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
      private interface ITaskbarList3
      {
         // ITaskbarList
         [PreserveSig]
         void HrInit();

         [PreserveSig]
         void AddTab(IntPtr hWnd);

         [PreserveSig]
         void DeleteTab(IntPtr hWnd);

         [PreserveSig]
         void ActivateTab(IntPtr hWnd);

         [PreserveSig]
         void SetActiveAlt(IntPtr hWnd);

         // ITaskbarList2
         [PreserveSig]
         void MarkFullscreenWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

         // ITaskbarList3
         void SetProgressValue(IntPtr hWnd, UInt64 ullCompleted, UInt64 ullTotal);
         void SetProgressState(IntPtr hWnd, ThumbnailProgressState tbpFlags);
      }

      [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
      [ClassInterface(ClassInterfaceType.None)]
      [ComImport]
      private class CTaskbarList
      {
      }

      /// <summary>
      /// The progress bar state for Windows Vista & 7
      /// </summary>
      public enum ProgressBarState
      {
         /// <summary>
         /// Indicates normal progress
         /// </summary>
         Normal = 1,

         /// <summary>
         /// Indicates an error in the progress
         /// </summary>
         Error = 2,

         /// <summary>
         /// Indicates paused progress
         /// </summary>
         Pause = 3
      }

      /// <summary>
      /// Represents the thumbnail progress bar state.
      /// </summary>
      public enum ThumbnailProgressState
      {
         /// <summary>
         /// No progress is displayed.
         /// </summary>
         NoProgress = 0,

         /// <summary>
         /// The progress is indeterminate (marquee).
         /// </summary>
         Indeterminate = 0x1,

         /// <summary>
         /// Normal progress is displayed.
         /// </summary>
         Normal = 0x2,

         /// <summary>
         /// An error occurred (red).
         /// </summary>
         Error = 0x4,

         /// <summary>
         /// The operation is paused (yellow).
         /// </summary>
         Paused = 0x8
      }
   }
}
