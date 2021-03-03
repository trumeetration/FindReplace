﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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

        private bool _isChecked = true;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        public void OpenFolder()
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\Users";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                MessageBox.Show("You selected: " + dialog.FileName);
                FolderPath = dialog.FileName;
            }
        }

        public ICommand PickFolderCommand
        {
            get => new RelayCommand(OpenFolder, () => true);
        }

        public ICommand IncludeSubDirsCommand
        {
            get => new RelayCommand(() => _isChecked = !_isChecked, () => true); 
        }
    }
}
