using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using Hiale.NgenGui.Helper;

namespace Hiale.NgenGui
{
    public class NgenFileItem : INotifyPropertyChanged
    {
        private AssemblyDetails _assemblyDetails;
        private string _assemblyFile;

        private ImageSource _fileIcon;

        private NgenFileStatus _status;

        public NgenFileItem(AssemblyDetails assemblyDetails, NgenFileStatus status)
        {
            AssemblyDetails = assemblyDetails;
            Status = status;
            AssemblyFile = Path.GetFileName(assemblyDetails.AssemblyFile);
        }

        public AssemblyDetails AssemblyDetails
        {
            get { return _assemblyDetails; }
            set
            {
                if (_assemblyDetails == value)
                    return;
                _assemblyDetails = value;
                FileIcon = ShellIcon.GetIcon(value.AssemblyFile);
                FileIcon.Freeze(); //http://stackoverflow.com/questions/4705726/xaml-bind-bitmapimage-viewmodel-property
                OnPropertyChanged("AssemblyDetails");
            }
        }

        public NgenFileStatus Status
        {
            get { return _status; }
            set
            {
                if (_status == value)
                    return;
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        public string AssemblyFile
        {
            get { return _assemblyFile; }
            set
            {
                if (_assemblyFile == value)
                    return;
                _assemblyFile = value;
                OnPropertyChanged("AssemblyFile");
            }
        }

        public ImageSource FileIcon
        {
            get { return _fileIcon; }
            set
            {
                if (_fileIcon == value)
                    return;
                _fileIcon = value;
                OnPropertyChanged("FileIcon");
            }
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
    }
}