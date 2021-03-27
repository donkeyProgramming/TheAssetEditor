using Common;
using CommonControls.Common;
using CommonControls.Services;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CommonControls.PackFileBrowser
{
    public abstract class ContextMenuHandler : NotifyPropertyChangedImpl
    {
        ObservableCollection<PackTreeContextMenuItem> _contextMenu;
        public ObservableCollection<PackTreeContextMenuItem> Items { get => _contextMenu; set => SetAndNotify(ref _contextMenu, value); }

        public ICommand RenameNodeCommand { get; set; }
        public ICommand AddFilesFromDirectory { get; set; }
        public ICommand AddFilesCommand { get; set; }
        public ICommand CloseNodeCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand SavePackFileCommand { get; set; }
        public ICommand CopyNodePathCommand { get; set; }
        public ICommand CopyToEditablePackCommand { get; set; }
        public ICommand DuplicateCommand { get; set; }
        public ICommand CreateFolderCommand { get; set; }
        public ICommand SetAsEditabelPackCommand { get; set; }
        public ICommand ExpandAllChildrenCommand { get; set; }
        public ICommand OpenToolCommand_Preview { get; set; }
        public ICommand OpenToolCommand_Text { get; set; }
        public ICommand OpenToolCommand_Kitbash { get; set; }
        public ICommand SavePackFileAsCommand { get; set; }

        protected PackFileService _packFileService;

        protected TreeNode _selectedNode;
        public ContextMenuHandler(PackFileService pf)
        {
            _packFileService = pf;

            RenameNodeCommand = new RelayCommand(OnRenameNode);
            AddFilesCommand = new RelayCommand(OnAddFilesCommand);
            AddFilesFromDirectory = new RelayCommand(OnAddFilesFromDirectory);
            DuplicateCommand = new RelayCommand(DuplicateNode);
            CreateFolderCommand = new RelayCommand(CreateFolder);
            CloseNodeCommand = new RelayCommand(CloseNode);
            DeleteCommand = new RelayCommand(DeleteNode);
            SavePackFileCommand = new RelayCommand(SavePackFile);
            SavePackFileAsCommand = new RelayCommand(SaveAsPackFile);
            CopyNodePathCommand = new RelayCommand(CopyNodePath);
            CopyToEditablePackCommand = new RelayCommand(CopyToEditablePack);
            SetAsEditabelPackCommand = new RelayCommand(SetAsEditabelPack);
            ExpandAllChildrenCommand = new RelayCommand(ExpandAllChildren);
        }

        void OnRenameNode()
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            if (_selectedNode.NodeType == NodeType.Directory)
            {
                MessageBox.Show("Not possible to rename a directory at this point");
            }
            else if (_selectedNode.NodeType == NodeType.File)
            {
                TextInputWindow window = new TextInputWindow("Rename file", _selectedNode.Item.Name);
                if (window.ShowDialog() == true)
                    _packFileService.RenameFile(_selectedNode.FileOwner, _selectedNode.Item, window.TextValue);
            }
        }

        void OnAddFilesCommand()
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = false;
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var parentPath = _selectedNode.GetFullPath();
                var files = dialog.FileNames;
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var packFile = new PackFile(fileName, new MemorySource(File.ReadAllBytes(file)));
                    _packFileService.AddFileToPack(_selectedNode.FileOwner, parentPath, packFile);
                }
            }
        }

        void OnAddFilesFromDirectory()
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var parentPath = _selectedNode.GetFullPath();
                _packFileService.AddFolderContent(_selectedNode.FileOwner, parentPath, dialog.FileName);
            }
        }

        void DuplicateNode()
        {
            var fileName = Path.GetFileNameWithoutExtension(_selectedNode.Name);
            var extention = Path.GetExtension(_selectedNode.Name);
            var newName = fileName + "_copy" + extention;

            var bytes = (_selectedNode.Item as PackFile).DataSource.ReadData();
            var packFile = new PackFile(newName, new MemorySource(bytes));

            var parentPath = _selectedNode.GetFullPath();
            var path = Path.GetDirectoryName(parentPath);

            _packFileService.AddFileToPack(_selectedNode.FileOwner, path, packFile);
        }

        void CreateFolder()
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            _selectedNode.Children.Add(new TreeNode("TestF", NodeType.Directory, _selectedNode.FileOwner, _selectedNode));


        }

        void DeleteNode()
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete the file?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if(_selectedNode.NodeType == NodeType.File)
                    _packFileService.DeleteFile(_selectedNode.FileOwner, _selectedNode.Item);
                else if (_selectedNode.NodeType == NodeType.Directory)
                    _packFileService.DeleteFolder(_selectedNode.FileOwner, _selectedNode.GetFullPath());
            }
        }

        void CloseNode()
        {
            if (MessageBox.Show("Are you sure you want to close the packfile?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                _packFileService.UnloadPackContainer(_selectedNode.FileOwner);
        }

        void SavePackFile()
        {
            var systemPath = _selectedNode.FileOwner.SystemFilePath;
            if (string.IsNullOrWhiteSpace(systemPath))
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = _selectedNode.FileOwner.Name;
                saveFileDialog.Filter = "PackFile | *.pack";
                saveFileDialog.DefaultExt = "pack";
                if (saveFileDialog.ShowDialog() == false)
                    return;
                systemPath = saveFileDialog.FileName;
            }

            _packFileService.Save(_selectedNode.FileOwner, systemPath, true);
            _selectedNode.UnsavedChanged = false;
            _selectedNode.ForeachNode((node) => node.UnsavedChanged = false);
        }

        void SaveAsPackFile()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = _selectedNode.FileOwner.Name;
            saveFileDialog.Filter = "PackFile | *.pack";
            saveFileDialog.DefaultExt = "pack";
            if (saveFileDialog.ShowDialog() == false)
                return;

            _packFileService.Save(_selectedNode.FileOwner, saveFileDialog.FileName, true);
            _selectedNode.UnsavedChanged = false;
            _selectedNode.ForeachNode((node) => node.UnsavedChanged = false);
        }

        void CopyNodePath()
        {
            if (_selectedNode.Item != null)
            {
                var path = _packFileService.GetFullPath(_selectedNode.Item as PackFile);
                Clipboard.SetText(path);
            }
        }

        void CopyToEditablePack()
        {
            if (_packFileService.GetEditablePack() == null)
            {
                MessageBox.Show("No editable pack selected!");
                return;
            }

            using (new WaitCursor())
            {
                var files = _selectedNode.GetAllChildFileNodes();
                foreach (var file in files)
                    _packFileService.CopyFileFromOtherPackFile(file.FileOwner, file.GetFullPath(), _packFileService.GetEditablePack());
            }
        }

        void SetAsEditabelPack()
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }
            
            _packFileService.SetEditablePack(_selectedNode.FileOwner);
        }

        void ExpandAllChildren()
        {
            ExpandAllRecursive(_selectedNode);
        }

        void ExpandAllRecursive(TreeNode node)
        {
            node.IsNodeExpanded = true;
            foreach (var child in node.Children)
                ExpandAllRecursive(child);
        }


        public abstract void Create(TreeNode node);
       

        protected PackTreeContextMenuItem Additem(ContextItems type, PackTreeContextMenuItem parent)
        {
            var item = GetItem(type);
            parent.ContextMenu.Add(item);
            return item;
        }

        protected PackTreeContextMenuItem Additem(ContextItems type, ObservableCollection<PackTreeContextMenuItem> parent)
        {
            var item = GetItem(type);
            parent.Add(item);
            return item;
        }

        protected void AddSeperator(ObservableCollection<PackTreeContextMenuItem> parent)
        {
            parent.Add(null);
        }

        PackTreeContextMenuItem GetItem(ContextItems type)
        {
            switch (type)
            {
                case ContextItems.Add:
                    return new PackTreeContextMenuItem() { Name = "Add"};
                case ContextItems.AddFiles:
                    return new PackTreeContextMenuItem() { Name = "Add file", Command = AddFilesCommand }; ;
                case ContextItems.AddDirectory:
                    return new PackTreeContextMenuItem() { Name = "Add directory", Command = AddFilesFromDirectory };
                case ContextItems.CopyToEditablePack:
                    return new PackTreeContextMenuItem() { Name = "Copy to Editable pack", Command = CopyToEditablePackCommand }; ;
                case ContextItems.Duplicate:
                    return new PackTreeContextMenuItem() { Name = "Duplicate", Command = DuplicateCommand }; ;
                case ContextItems.CreateFolder:
                    return new PackTreeContextMenuItem() { Name = "Create Folder", Command = CreateFolderCommand }; ;
                case ContextItems.Expand:
                    return new PackTreeContextMenuItem() { Name = "Expand", Command = ExpandAllChildrenCommand }; ;
                case ContextItems.CopyFullPath:
                    return new PackTreeContextMenuItem() { Name = "Copy full path", Command = CopyNodePathCommand };
                case ContextItems.Rename:
                    return new PackTreeContextMenuItem() { Name = "Rename", Command = RenameNodeCommand }; ;
                case ContextItems.SetAsEditabelPack:
                    return new PackTreeContextMenuItem() { Name = "Set as Editable pack", Command = SetAsEditabelPackCommand }; 
                case ContextItems.Delete:
                    return new PackTreeContextMenuItem() { Name = "Delete", Command = DeleteCommand }; ;
                case ContextItems.Close:
                    return new PackTreeContextMenuItem() { Name = "Close", Command = CloseNodeCommand }; ;
                case ContextItems.Save:
                    return new PackTreeContextMenuItem() { Name = "Save", Command= SavePackFileCommand }; ;
                case ContextItems.SaveAs:
                    return new PackTreeContextMenuItem() { Name = "Save as", Command = SavePackFileAsCommand }; ;
                case ContextItems.Open:
                    return new PackTreeContextMenuItem() { Name = "Open", }; ;
                case ContextItems.OpenWithTextEditor:
                    return new PackTreeContextMenuItem() { Name = "Text editor", Command = OpenToolCommand_Text }; ;
                case ContextItems.OpenWithKitbasher:
                    return new PackTreeContextMenuItem() { Name = "Kitbasher", Command = OpenToolCommand_Kitbash }; ;
                case ContextItems.OpenWithPreview:
                    return new PackTreeContextMenuItem() { Name = "Preview tool", Command = OpenToolCommand_Preview }; ;
            }

            throw new Exception("Unknown ContextItems type ");
        }

        protected enum ContextItems
        { 
            Add,
            AddFiles,
            AddDirectory,
            CopyToEditablePack,
            Duplicate,
            CreateFolder,

            Expand,
            CopyFullPath,
            Rename,
            SetAsEditabelPack,

            Delete,
            Close,
            Save,
            SaveAs,

            Open,
            OpenWithTextEditor,
            OpenWithKitbasher,
            OpenWithPreview,
        }
    }

    public class PackTreeContextMenuItem
    {
        public string Name { get; set; }
        public ICommand Command { get; set; }
        public ObservableCollection<PackTreeContextMenuItem> ContextMenu { get; set; } = new ObservableCollection<PackTreeContextMenuItem>();
    }
}
