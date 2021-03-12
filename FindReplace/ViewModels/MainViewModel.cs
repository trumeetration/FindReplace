using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FindReplace.Commands;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace FindReplace.ViewModels
{
    public class MainViewModel:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public MainViewModel()
        {
            ProgressMsg = $"Progress: {RenderedItemsCount} / {TotalItems}";
        }

        private void BwOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            RenderedItemsCount = (int) e.UserState;
            ProgressState = e.ProgressPercentage;
            ProgressMsg = $"Progress: {RenderedItemsCount} / {TotalItems}";
        }

        private void BwOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsBusy = false;
            _bw.DoWork -= BwOnDoWork;
            _bw.RunWorkerCompleted -= BwOnRunWorkerCompleted;
            if (e.Cancelled)
            {
                ProgressState = 0;
                TotalItems = 0;
                RenderedItemsCount = 0;
                ProgressMsg = $"Progress: {RenderedItemsCount} / {TotalItems}";
            }
            _bw.ProgressChanged -= BwOnProgressChanged;
            CommandManager.InvalidateRequerySuggested();
        }

        private void BwOnDoWork(object sender, DoWorkEventArgs e)
        {
            RenderedItemsCount = 0;
            var viewmodel = (MainViewModel) e.Argument;
            var bw = (BackgroundWorker) sender;
            string[] filesPathes = Directory.GetFiles(FolderPath,
                string.IsNullOrWhiteSpace(FileMask) ? "*.*" : FileMask,
                SearchOption.AllDirectories);
            if (filesPathes.Length == 0)
            {
                e.Cancel = true;
            }
            else
            {
                var PathCollection = GetFilteredPathList(filesPathes);
                /*TotalItems = filesPathes.Length;*/
                for (int i = 0; PathCollection.MoveNext(); i++)
                {
                    if (_bw.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                    RenderedItemsCount++;
                    bw.ReportProgress((int)((i + 1) / (float)filesPathes.Length * 100), i + 1);
                    Thread.Sleep(1);
                }
            }

        }

        private IEnumerator<string> GetFilteredPathList(string[] filesPathes)
        {
            var tmp = filesPathes.Where(x => IsExcluded(x) == false);
            TotalItems = tmp.Count();
            return tmp.GetEnumerator();
        }

        private bool IsExcluded(string path)
        {
            return new Regex(ExcludeFileMask.Replace(".", "[.]").Replace("*", ".*").Replace("?", ".")).IsMatch(path)
                   || path.Contains($"/{ExcludeDir}/");
        }

        private string _folderPath;

        public string FolderPath
        {
            get => _folderPath;
            set
            {
                _folderPath = value;
                OnPropertyChanged(nameof(FolderPath));
            }
        }

        private int _progress;

        public int ProgressState
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(ProgressState));
            }
        }

        private int _rendered;

        private string _progressMsg;

        public string ProgressMsg
        {
            get => _progressMsg;
            set
            {
                _progressMsg = value;
                OnPropertyChanged(nameof(ProgressMsg));
            }
        }

        public int RenderedItemsCount
        {
            get => _rendered;
            set
            {
                _rendered = value;
                OnPropertyChanged(nameof(RenderedItemsCount));
            }
        }

        private int _totalItems;

        public int TotalItems
        {
            get => _totalItems;
            set
            {
                _totalItems = value;
                OnPropertyChanged(nameof(TotalItems));
            }
        }

        private string _fileMask;

        public string FileMask
        {
            get => _fileMask;
            set
            {
                _fileMask = value;
                OnPropertyChanged(nameof(FileMask));
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        private string _exDir;

        public string ExcludeDir
        {
            get => _exDir;
            set
            {
                _exDir = value;
                OnPropertyChanged(nameof(ExcludeDir));
            }
        }

        private string _exFileMask;

        public string ExcludeFileMask
        {
            get => _exFileMask;
            set
            {
                _exFileMask = value;
                OnPropertyChanged(nameof(ExcludeFileMask));
            }
        }

        private bool _includeSubDirs;

        public bool IncludeSubDirs
        {
            get => _includeSubDirs;
            set
            {
                _includeSubDirs = value;
                OnPropertyChanged(nameof(_includeSubDirs));
            }
        }

        public void OpenFolder()
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                FolderPath = dialog.FileName;
            }
        }

        public ICommand PickFolderCommand
        {
            get => new RelayCommand(OpenFolder, () => true);
        }

        public ICommand FindDataCommand
        {
            get => new RelayCommand(() =>
            {
                _bw.DoWork += BwOnDoWork;
                _bw.RunWorkerCompleted += BwOnRunWorkerCompleted;
                _bw.ProgressChanged += BwOnProgressChanged;
                IsBusy = true;
                _bw.RunWorkerAsync(this);
            }, () => IsBusy == false && string.IsNullOrWhiteSpace(FolderPath) == false);
        }

        public ICommand CancelWork
        {
            get => new RelayCommand(() =>
            {
                _bw.CancelAsync();
            }, () => IsBusy);
        }

        private BackgroundWorker _bw = new BackgroundWorker()
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };
    }
}
