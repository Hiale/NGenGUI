using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hiale.NgenGui
{
    public class AppLogic
    {
        public delegate void AppStatusChangeHandler(AppStatus status);

        public delegate void AppProgessChangedHandler(int value, int maximum);

        public delegate void ExceptionHandler(Exception e);

        private AppStatus _status;

        public AppLogic()
        {
            FileList = new AsyncObservableCollection<NgenFileItem>();
        }

        public AsyncObservableCollection<NgenFileItem> FileList { get; private set; }

        public AppStatus Status
        {
            get { return _status; }
            private set
            {
                if (_status == value)
                    return;
                _status = value;
                OnStatusChanged(value);
            }
        }

        public event AppStatusChangeHandler StatusChanged;
        public event AppProgessChangedHandler ProgressChanged;
        public event ExceptionHandler ExceptionOccurred;

        private static readonly object Locker = new object();

        public void AddFiles(string[] fileNames)
        {
            var t = new Thread(RunAddFiles) {Name = "NGen Run - Files", IsBackground = true};
            t.Start(fileNames);
        }

        public void AddDirectory(string path, bool includeSubDirs)
        {
            var t = new Thread(RunAddDirectory) {Name = "NGen Run - Directory", IsBackground = true};
            t.Start(new object[] {path, includeSubDirs, true});
        }


        public void Install()
        {
            Install(FileList);
        }

        public void Install(IList<NgenFileItem> files)
        {
            var t = new Thread(RunMainTask) {Name = "NGen Run - Install", IsBackground = true};
            t.Start(new object[] {true, files});
        }

        public void Deinstall()
        {
            Deinstall(FileList);
        }

        public void Deinstall(IList<NgenFileItem> files)
        {
            var t = new Thread(RunMainTask) {Name = "NGen Run - Uninstall", IsBackground = true};
            t.Start(new object[] {false, files});
        }

        public void Stop()
        {
            Status = AppStatus.Stopping;
        }

        private void RunAddFiles(object filenameArray)
        {
            try
            {
                SwitchStatus();
                var files = (string[]) filenameArray;
                CheckFiles(files);
            }
            catch (Exception ex)
            {
                if (ExceptionOccurred != null)
                    ExceptionOccurred(ex);
            }
            finally
            {
                SwitchStatus();
            }
        }

        private void RunAddDirectory(object data)
        {
            var pathData = ((object[]) data);
            var path = (string) pathData[0];
            var includeSubDirs = (bool) pathData[1];
            var switchStatus = (bool) pathData[2];
            try
            {
                if (switchStatus)
                    SwitchStatus();
                var files = Directory.GetFiles(path, "*.*").Where(s => s.EndsWith(".dll") || s.EndsWith(".exe"));
                CheckFiles(files);
                if (includeSubDirs)
                {
                    var dirs = Directory.GetDirectories(path);
                    foreach (var dir in dirs)
                    {
                        RunAddDirectory(new object[] {dir, true, false});
                    }
                }
            }
            catch (Exception ex)
            {
                if (ExceptionOccurred != null)
                    ExceptionOccurred(ex);
            }
            finally
            {
                if ((switchStatus))
                    SwitchStatus();
            }
        }

        private void CheckFiles(IEnumerable<string> files)
        {
            var startIndex = FileList.Count;
            foreach (var file in files)
            {
                if (Status == AppStatus.Stopping)
                    return;
                if (FileList.Any(item => file.ToLower() == item.AssemblyDetails.AssemblyFile.ToLower()))
                    continue;
                var assembly = AssemblyDetails.FromFile(file);
                if (assembly == null)
                    continue;
                FileList.Add(new NgenFileItem(assembly, NgenFileStatus.Unknown));
            }

            var processedFiles = 0;
            Parallel.For(startIndex, FileList.Count, i =>
                {
                    var file = FileList[i];
                    if (Status == AppStatus.Stopping)
                        return;
                    file.Status = NgenFileStatus.InProgress;
                    var ngen = new NgenProcess(file.AssemblyDetails);
                    file.Status = ngen.Check() ? NgenFileStatus.Installed : NgenFileStatus.Deinstalled;

                    lock (Locker)
                    {
                        processedFiles++;
                        OnProgressChanged(processedFiles, FileList.Count);
                    }
                });
        }

        private void SwitchStatus()
        {
            switch (Status)
            {
                case AppStatus.Idle:
                    Status = AppStatus.Busy;
                    break;
                case AppStatus.Busy:
                    Status = AppStatus.Idle;
                    break;
                case AppStatus.Stopping:
                    Status = AppStatus.Idle;
                    break;
            }
        }

        private void RunMainTask(object parameter)
        {
            SwitchStatus();
            var installMode = (bool) ((object[]) parameter)[0];
            var items = (IList<NgenFileItem>) ((object[]) parameter)[1];
            var previousStatus = new NgenFileStatus[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                var file = items[i];
                previousStatus[i] = file.Status;
                file.Status = NgenFileStatus.Pending;
            }

            var processedFiles = 0;
            Parallel.For(0, items.Count, (currentIndex, state) =>
                {
                    if (Status == AppStatus.Stopping)
                        state.Stop();
                    var file = items[currentIndex];
                    file.Status = NgenFileStatus.InProgress;
                    var ngen = new NgenProcess(file.AssemblyDetails);
                    var installed = ngen.Check();
                    if (installMode && !installed)
                    {
                        ngen.Install();
                        installed = ngen.Check();
                    }
                    else if (!installMode && installed)
                    {
                        ngen.Uninstall();
                        installed = ngen.Check();
                    }
                    file.Status = installed ? NgenFileStatus.Installed : NgenFileStatus.Deinstalled;
                    lock (Locker)
                    {
                        processedFiles++;
                        OnProgressChanged(processedFiles, FileList.Count);
                    }
                }
                );
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Status == NgenFileStatus.Pending)
                    items[i].Status = previousStatus[i];
            }
            SwitchStatus();
        }

        public void RemoveFiles(IList<NgenFileItem> files)
        {
            if (files.Count < 1)
            {
                FileList.Clear();
                return;
            }
            var itemsToRemove = new NgenFileItem[files.Count];
            for (var i = 0; i < files.Count; i++)
                itemsToRemove[i] = files[i];
            foreach (var item in itemsToRemove)
                FileList.Remove(item);
        }

        private void OnStatusChanged(AppStatus status)
        {
            if (StatusChanged != null)
                StatusChanged(status);
        }

        private void OnProgressChanged(int value, int maximum)
        {
            if (ProgressChanged != null)
                ProgressChanged(value, maximum);
        }
    }
}