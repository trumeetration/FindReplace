using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;
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

        private BackgroundWorker _bw;
        public ObservableCollection<FileData> FileCollection { get; set; }

        public MainViewModel()
        {
            ProgressMsg = $"Progress: {RenderedItemsCount} / {TotalItems}";
            _bw = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _bw.DoWork += BwOnDoWork;
            _bw.RunWorkerCompleted += BwOnRunWorkerCompleted;
            _bw.ProgressChanged += BwOnProgressChanged;
            FileCollection = new ObservableCollection<FileData>();
            ExcludeDir = ExcludeFileMask = string.Empty;
        }

        private void BwOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if ((FileData)e.UserState != null)
            {
                FileCollection.Add((FileData)e.UserState);
            }
            ProgressState = e.ProgressPercentage;
            ProgressMsg = $"Progress: {RenderedItemsCount} / {TotalItems}";
        }

        private void BwOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsBusy = false;
            if (e.Cancelled)
            {
                ProgressState = 0;
                TotalItems = 0;
                RenderedItemsCount = 0;
                ProgressMsg = $"Progress: {RenderedItemsCount} / {TotalItems}";
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void BwOnDoWork(object sender, DoWorkEventArgs e)
        {
            RenderedItemsCount = 0;
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
                TotalItems = PathCollection.Count();
                for (int i = 0; i < PathCollection.Count(); i++)
                {
                    if (_bw.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                    RenderFile(PathCollection.ElementAt(i), (int)((i + 1) / (float)PathCollection.Count() * 100), bw);
                    RenderedItemsCount++;
                    bw.ReportProgress((int)((i + 1) / (float)PathCollection.Count() * 100));
                    Thread.Sleep(1000);
                }
            }

        }

        private void RenderFile(string path, int percentage,
            BackgroundWorker bw)
        {
            if (IsNeedToCorrect == false)
            {
                using (var input = File.OpenText(path))
                {
                    string line;
                    while ((line = input.ReadLine()) != null)
                    {
                        if (line.Contains(ToFindSubstring))
                        {
                            bw.ReportProgress(percentage, new FileData()
                                {
                                    Path = path,
                                    StringBefore = line
                                });
                        }
                    }
                }
            }
            else
            {
                bool isSubstrFound = false;
                using (var input = File.OpenText(path))
                {
                    string line;
                    while ((line = input.ReadLine()) != null)
                    {
                        if (line.Contains(ToFindSubstring)) 
                        {
                            isSubstrFound = true;
                            bw.ReportProgress(percentage, new FileData()
                                {
                                    Path = path,
                                    StringBefore = line,
                                    StringAfter = line.Replace(ToFindSubstring, ReplaceWithSubstring)
                            });
                        }
                    }
                }
                if (isSubstrFound == true)
                {
                    using (var input = File.OpenText(path))
                    using (var output = new StreamWriter(path + ".tmp"))
                    {
                        string line;
                        while ((line = input.ReadLine()) != null)
                        {
                            line = line.Contains(ToFindSubstring) ? line.Replace(ToFindSubstring, ReplaceWithSubstring) : line; 
                            output.WriteLine(line);
                        }
                    }
                    File.Replace(path + ".tmp", path, null);
                }
            }
        }

        private IEnumerable<string> GetFilteredPathList(string[] filesPathes)
        {
            return filesPathes.Where(x => IsExcluded(x) == false);
        }

        private bool IsExcluded(string path)
        { 
            if (ExcludeFileMask != string.Empty)
                return new Regex(ExcludeFileMask.Replace(".", "[.]").Replace("*", ".*").Replace("?", ".")).IsMatch(path)
                        || path.Contains($"/{ExcludeDir}/");
            return path.Contains($"/{ExcludeDir}/");
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

        private int _rendered;
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

        private bool _isNeedToCorrect;
        public bool IsNeedToCorrect
        {
            get => _isNeedToCorrect;
            set
            {
                _isNeedToCorrect = value;
                OnPropertyChanged(nameof(IsNeedToCorrect));
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

        private string _toFind;
        public string ToFindSubstring
        {
            get => _toFind;
            set
            {
                _toFind = value;
                OnPropertyChanged(nameof(ToFindSubstring));
            }
        }

        private string _replaceWith;
        public string ReplaceWithSubstring
        {
            get => _replaceWith;
            set
            {
                _replaceWith = value;
                OnPropertyChanged(nameof(ReplaceWithSubstring));
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
                IsBusy = true;
                IsNeedToCorrect = false;
                FileCollection.Clear();
                _bw.RunWorkerAsync(this);
            }, () => IsBusy == false
                     && string.IsNullOrWhiteSpace(FolderPath) == false
                     && string.IsNullOrWhiteSpace(ToFindSubstring) == false);
        }

        public ICommand CorrectDataCommand
        {
            get => new RelayCommand(() =>
            {
                IsBusy = true;
                IsNeedToCorrect = true;
                FileCollection.Clear();
                _bw.RunWorkerAsync(this);
            }, () => IsBusy == false 
                     && string.IsNullOrWhiteSpace(FolderPath) == false
                     && string.IsNullOrWhiteSpace(ToFindSubstring) == false; 
        }

        public ICommand CancelWork
        {
            get => new RelayCommand(() =>
            {
                _bw.CancelAsync();
            }, () => IsBusy);
        }

        private FileData _selectedFile;
        public FileData SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (_selectedFile != value)
                {
                    _selectedFile = value;
                    OnPropertyChanged(nameof(SelectedFile));
                }
            }
        }
    }

    public class FileData
    {
        public string Path { get; set; }
        public string StringBefore { get; set; }
        public string StringAfter { get; set; }

    }
}
