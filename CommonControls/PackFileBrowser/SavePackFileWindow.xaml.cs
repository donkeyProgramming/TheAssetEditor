using Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CommonControls.PackFileBrowser
{

    public partial class SavePackFileWindow : Window, IDisposable, INotifyPropertyChanged
    {
        public PackFile SelectedFile { get; set; }
        public PackFileBrowserViewModel ViewModel { get; set; }

        TreeNode _selectedNode;
        string _currentFileName;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentFileName { get => _currentFileName; set { _currentFileName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentFileName")); SelectedFile = null; } }
        PackFileService _packfileService;


        public string FilePath { get; private set; }
        public SavePackFileWindow(PackFileService packfileService)
        {
            _packfileService = packfileService;
            ViewModel = new PackFileBrowserViewModel(packfileService, true);
            ViewModel.ContextMenu = new OpenFileContexMenuHandler(packfileService);
            ViewModel.FileOpen += ViewModel_FileOpen;
            ViewModel.NodeSelected += ViewModel_FileSelected;
            InitializeComponent();
            DataContext = this;
        }

        private void ViewModel_FileSelected(TreeNode node)
        {
            _selectedNode = node;

            if (_selectedNode.Item == null)
                CurrentFileName = "";
            else
                CurrentFileName = _selectedNode.Item.Name;

            SelectedFile = _selectedNode.Item as PackFile;
        }

        private void ViewModel_FileOpen(IPackFile file)
        {
            SelectedFile = file as PackFile;
            Button_Click(null, null);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentFileName))
            {
                MessageBox.Show("No name provided, can not save file");
                return;
            }

            if (SelectedFile != null)
            {
                if(MessageBox.Show("Replace file?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
            }

            string path = "";
            if (SelectedFile != null)
            {
                path = _packfileService.GetFullPath(SelectedFile);
            }
            else
            {
                if(_selectedNode == null)
                {
                    path = "";
                }
                else
                if (_selectedNode.NodeType == NodeType.File)
                {
                    var fullPath = _selectedNode.GetFullPath();
                    path = System.IO.Path.GetDirectoryName(fullPath) + "\\";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_selectedNode?.Name) == false)
                    {
                        var fullPath = _selectedNode.GetFullPath();
                        //var fullPath = _packfileService.GetFullPath(_selectedNode.Item as PackFile, _selectedNode.FileOwner);
                        //fullPath = System.IO.Path.GetDirectoryName(fullPath);
                        path = fullPath + "\\";
                    }
                }

                path += CurrentFileName;
                path = path.ToLower();
            }

            FilePath = path;
            DialogResult = true;
            Close();
        }

        public void Dispose()
        {
            ViewModel.FileOpen -= ViewModel_FileOpen;
            ViewModel.NodeSelected -= ViewModel_FileSelected;
            ViewModel.Dispose();
            ViewModel = null;
            DataContext = null;
        }
    }
}
