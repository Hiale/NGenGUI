using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Hiale.NgenGui.FolderBrowserDialog;
using Hiale.NgenGui.Helper;
using Microsoft.Win32;

namespace Hiale.NgenGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly AppLogic _appLogic;

        private bool _cancelPossible;

        private readonly TaskbarProgressBar _progressBar;

        public MainWindow()
        {
            _appLogic = new AppLogic();
            _appLogic.ExceptionOccurred += ExceptionOccurred;
            _appLogic.StatusChanged += StatusChanged;
            _appLogic.ProgressChanged += ProgressChanged;
            InitializeComponent();
            if (!Misc.CheckAdmin())
            {
                MessageBox.Show("This application needs to run under admin privileges.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                Environment.Exit(-1);
            }
            var handle = new WindowInteropHelper(this).EnsureHandle();
            _progressBar = new TaskbarProgressBar(handle);
        }

        public AsyncObservableCollection<NgenFileItem> FileList
        {
            get { return _appLogic.FileList; }
        }

        private AppStatus Status
        {
            get { return _appLogic.Status; }
        }

        public bool CancelPossible
        {
            get { return _cancelPossible; }
            private set
            {
                if (_cancelPossible == value)
                    return;
                _cancelPossible = value;
                OnPropertyChanged("CancelPossible");
            }
        }

        #region Commands

        public static readonly RoutedCommand AddFilesCommand = new RoutedCommand("Add Files", typeof (MainWindow));

        public static readonly RoutedCommand AddDirectoryCommand = new RoutedCommand("Add Directory", typeof (MainWindow));

        public static readonly RoutedCommand RemoveFilesCommand = new RoutedCommand("Remove Files", typeof (MainWindow));

        public static readonly RoutedCommand ClearFileListCommand = new RoutedCommand("Clear File List", typeof (MainWindow));

        public static readonly RoutedCommand InstallCommand = new RoutedCommand("Compile to Native", typeof (MainWindow));

        public static readonly RoutedCommand DeinstallCommand = new RoutedCommand("Remove Native Build", typeof (MainWindow));

        public static readonly RoutedCommand CancelCommand = new RoutedCommand("Cancel Operation", typeof (MainWindow));

        #endregion

        private void ExceptionOccurred(Exception e)
        {
            if (Dispatcher.CheckAccess())
                MessageBox.Show(e.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            else
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action) (() => MessageBox.Show(e.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error)));
        }

        private void StatusChanged(AppStatus status)
        {
            OnPropertyChanged("Status");
            if (status != AppStatus.Stopping)
                CancelPossible = Status == AppStatus.Busy;
            if (Dispatcher.CheckAccess())
                CommandManager.InvalidateRequerySuggested();
            else
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(CommandManager.InvalidateRequerySuggested));
            switch (status)
            {
                case AppStatus.Busy:
                    _progressBar.ShowInTaskbar = true;
                    break;
                case AppStatus.Idle:
                    _progressBar.Value = 0;
                    _progressBar.ShowInTaskbar = false;
                    break;
                case AppStatus.Stopping:
                    _progressBar.State = TaskbarProgressBar.ProgressBarState.Pause;
                    break;
            }
        }

        private void ProgressChanged(int value, int maximum)
        {
            var percent = (float) value/maximum*100;
            _progressBar.Value = (int) Math.Round(percent);
        }

        private void MenuFileExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuHelpAboutClick(object sender, RoutedEventArgs e)
        {
            var asm = Assembly.GetExecutingAssembly();
            var sBuilder = new StringBuilder();
            sBuilder.Append(Title + " " + asm.GetName().Version);
            sBuilder.Append(Environment.NewLine);
            sBuilder.Append("Build on " + Misc.GetLinkerTimestamp(asm.Location));

            var attributes = asm.GetCustomAttributes(typeof (AssemblyCopyrightAttribute), false);
            if (attributes.Length > 0)
            {
                sBuilder.Append(Environment.NewLine);
                sBuilder.Append(((AssemblyCopyrightAttribute) attributes[0]).Copyright);
            }
            MessageBox.Show(sBuilder.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        private void AddFilesCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new OpenFileDialog {Multiselect = true, Filter = "Assembly (*.exe; *.dll) |*.exe;*.dll|All Files|*.*"};
            if ((bool) dlg.ShowDialog())
                _appLogic.AddFiles(dlg.FileNames);
        }

        private void AddFilesCommandCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Status == AppStatus.Idle)
                e.CanExecute = true;
        }

        private void AddDirectoryCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            //dialog.UseClassicDialog = true;
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true; // This applies to the Vista style dialog only, not the old dialog.
            dialog.ShowIncludeSubDirectories = true;
            dialog.IncludeSubDirectories = true;
            if ((bool) dialog.ShowDialog(this))
                _appLogic.AddDirectory(dialog.SelectedPath, dialog.IncludeSubDirectories);
        }

        private void AddDirectoryCommandCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Status == AppStatus.Idle)
                e.CanExecute = true;
        }

        private void InstallCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (lstItems.SelectedItems.Count > 0)
                _appLogic.Install(CreateFileList());
            else
                _appLogic.Install();
        }

        private void InstallCommandCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            FileListOperationHCanExecuted(e);
        }

        private void DeinstallCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (lstItems.SelectedItems.Count > 0)
                _appLogic.Deinstall(CreateFileList());
            else
                _appLogic.Deinstall();
        }

        private void DeinstallCommandCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            FileListOperationHCanExecuted(e);
        }

        private void CancelCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _appLogic.Stop();
        }

        private void CancelCommandCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Status == AppStatus.Busy)
                e.CanExecute = true;
        }

        private bool ConfirmRemove()
        {
            return MessageBox.Show("Remove selected files?", Title, MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        private void LstItemsKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && ConfirmRemove())
                _appLogic.RemoveFiles(CreateFileList());
        }

        private List<NgenFileItem> CreateFileList()
        {
            var newList = new List<NgenFileItem>(lstItems.SelectedItems.Count);
            newList.AddRange(lstItems.SelectedItems.Cast<NgenFileItem>());
            return newList;
        }

        private void RemoveFilesCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!ConfirmRemove())
                return;
            var newList = new List<NgenFileItem>(lstItems.SelectedItems.Count);
            newList.AddRange(lstItems.SelectedItems.Cast<NgenFileItem>());
            _appLogic.RemoveFiles(newList);
        }

        private void RemoveFilesCommandCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Status == AppStatus.Idle && CreateFileList().Count > 0;
        }

        private void ClearFileListExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show("Remove all files?", Title, MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No) == MessageBoxResult.Yes)
                FileList.Clear();
        }

        private void ClearFileListCommandCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            FileListOperationHCanExecuted(e);
        }

        private void FileListOperationHCanExecuted(CanExecuteRoutedEventArgs e)
        {
            if (Status == AppStatus.Idle && FileList.Count > 0)
                e.CanExecute = true;
        }
    }
}