using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common;
using Shared.Ui.Events.UiCommands;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu
{/*
    public abstract class ContextMenuHandler : NotifyPropertyChangedImpl
    {
        private readonly ILogger _logger = Logging.Create<ContextMenuHandler>();

        ObservableCollection<ContextMenuItem> _contextMenu;
        public ObservableCollection<ContextMenuItem> Items { get => _contextMenu; set => SetAndNotify(ref _contextMenu, value); }

        public ICommand RenameNodeCommand { get; set; }
        public ICommand AddFilesFromDirectory { get; set; }
        public ICommand AddFilesCommand { get; set; }
        public ICommand CloseNodeCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand SavePackFileCommand { get; set; }
        public ICommand CopyNodePathCommand { get; set; }
        public ICommand ExportToFolderCommand { get; set; }
        public ICommand AdvancedExportToFolderCommand { get; set; }
        public ICommand AdvancedImport { get; set; }
        public ICommand CopyToEditablePackCommand { get; set; }
        public ICommand DuplicateCommand { get; set; }
        public ICommand CreateFolderCommand { get; set; }
        public ICommand SetAsEditabelPackCommand { get; set; }
        public ICommand ExpandAllChildrenCommand { get; set; }
        public ICommand CollapseAllChildrenCommand { get; set; }
        public ICommand OpenPack_FileNotpadPluss_Command { get; set; }
        public ICommand OpenPackFile_HxD_Command { get; set; }
        public ICommand SavePackFileAsCommand { get; set; }

        protected IPackFileService _packFileService;

        protected TreeNode _selectedNode;
        private readonly IUiCommandFactory _uiCommandFactory;
        protected readonly IExportFileContextMenuHelper _exportFileContextMenuHelper;
        protected readonly IImportFileContextMenuHelper _importFileContextMenuHelper;

        public ContextMenuHandler(IPackFileService pf, IUiCommandFactory uiCommandFactory, IExportFileContextMenuHelper exportFileContextMenuHelper, IImportFileContextMenuHelper importtFileContextMenuHelper)
        {
            _packFileService = pf;
            _uiCommandFactory = uiCommandFactory;
            _exportFileContextMenuHelper = exportFileContextMenuHelper;
            _importFileContextMenuHelper = importtFileContextMenuHelper;
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
            CollapseAllChildrenCommand = new RelayCommand(CollapsAllChildren);
            ExportToFolderCommand = new RelayCommand(ExportToFolder);
            AdvancedExportToFolderCommand = new RelayCommand(AdvancedExportToFolder);
            AdvancedImport = new RelayCommand(OnAdvancedImport);

            OpenPack_FileNotpadPluss_Command = new RelayCommand(() => OpenPackFileUsing(@"C:\Program Files\Notepad++\notepad++.exe", _selectedNode.Item));
            OpenPackFile_HxD_Command = new RelayCommand(() => OpenPackFileUsing(@"C:\Program Files\HxD\HxD.exe", _selectedNode.Item));
        }

        void OnRenameNode()
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            if (_selectedNode.GetNodeType() == NodeType.Directory)
            {
                var newFolderName = EditFileNameDialog.ShowDialog(_selectedNode.Parent, _selectedNode.Name);
                if (newFolderName.Any())
                {
                    _selectedNode.Name = newFolderName;
                    _packFileService.RenameDirectory(_selectedNode.FileOwner, _selectedNode.GetFullPath(), newFolderName);
                }

            }
            else if (_selectedNode.GetNodeType() == NodeType.File)
            {
                var newFileName = EditFileNameDialog.ShowDialog(_selectedNode.Parent, _selectedNode.Name);
                if (newFileName.Any())
                    _packFileService.RenameFile(_selectedNode.FileOwner, _selectedNode.Item, newFileName);

            }
        }






        void OnAddFilesCommand()
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }

            var dialog = new OpenFileDialog()
            {
                Multiselect = true,
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var parentPath = _selectedNode.GetFullPath();
                var files = dialog.FileNames;
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var packFile = new PackFile(fileName, new MemorySource(File.ReadAllBytes(file)));
                    var item = new NewPackFileEntry(parentPath, packFile);
                    _packFileService.AddFilesToPack(_selectedNode.FileOwner, [item]);
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

            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var parentPath = _selectedNode.GetFullPath();
                var originalFilePaths = Directory.GetFiles(parentPath, "*", SearchOption.AllDirectories);
                var filePaths = originalFilePaths.Select(x => x.Replace(dialog.SelectedPath + "\\", "")).ToList();
                if (!string.IsNullOrWhiteSpace(parentPath))
                    parentPath += "\\";

                var filesAdded = new List<NewPackFileEntry>();
                for (var i = 0; i < filePaths.Count; i++)
                {
                    var currentPath = filePaths[i];
                    var filename = Path.GetFileName(currentPath);

                    var source = MemorySource.FromFile(originalFilePaths[i]);
                    var file = new PackFile(filename, source);
                    filesAdded.Add(new NewPackFileEntry(parentPath.ToLower(), file));

                }

                _packFileService.AddFilesToPack(_selectedNode.FileOwner, filesAdded);
            }

        }

        void DuplicateNode()
        {
            //_uiCommandFactory.Create<DuplicateFileCommand>().Execute(_selectedNode.Item);
        }

        void CreateFolder()
        {
            if (_selectedNode.FileOwner.IsCaPackFile)
            {
                MessageBox.Show("Unable to edit CA packfile");
                return;
            }


            var folderName = EditFileNameDialog.ShowDialog(_selectedNode, "");

            if (folderName.Any())
                _selectedNode.Children.Add(new TreeNode(folderName, NodeType.Directory, _selectedNode.FileOwner, _selectedNode));

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
                if (_selectedNode.GetNodeType() == NodeType.File)
                    _packFileService.DeleteFile(_selectedNode.FileOwner, _selectedNode.Item);
                else if (_selectedNode.GetNodeType() == NodeType.Directory)
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
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = _selectedNode.FileOwner.Name;
                saveFileDialog.Filter = "PackFile | *.pack";
                saveFileDialog.DefaultExt = "pack";
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    return;
                systemPath = saveFileDialog.FileName;
            }

            using (new WaitCursor())
            {
                try
                {
                    _packFileService.SavePackContainer(_selectedNode.FileOwner, systemPath, false);
                    _selectedNode.UnsavedChanged = false;
                    _selectedNode.ForeachNode((node) => node.UnsavedChanged = false);
                }
                catch (Exception e)
                {
                    _logger.Here().Error(e, "Exception while saving");
                    MessageBox.Show("Error saving:\n\n" + e.Message, "Error");
                }
            }
        }

        void SaveAsPackFile()
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = _selectedNode.FileOwner.Name;
            saveFileDialog.Filter = "PackFile | *.pack";
            saveFileDialog.DefaultExt = "pack";
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            using (new WaitCursor())
            {
                _packFileService.SavePackContainer(_selectedNode.FileOwner, saveFileDialog.FileName, false);
                _selectedNode.UnsavedChanged = false;
                _selectedNode.ForeachNode((node) => node.UnsavedChanged = false);
            }
        }

        void CopyNodePath()
        {
            if (_selectedNode.Item != null)
            {
                var path = _packFileService.GetFullPath(_selectedNode.Item);
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

        void ExpandAllChildren() => ExpandAllRecursive(_selectedNode);
        void CollapsAllChildren() => CollapsAllRecursive(_selectedNode);

        void ExportToFolder()
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var nodeStartDir = Path.GetDirectoryName(_selectedNode.GetFullPath());
                var fileCounter = 0;
                SaveSelfAndChildren(_selectedNode, dialog.SelectedPath, nodeStartDir, ref fileCounter);
                MessageBox.Show($"{fileCounter} files exported!");
            }
        }

        void AdvancedExportToFolder()
        {
            _exportFileContextMenuHelper.ShowDialog(_selectedNode.Item);
        }

        void OnAdvancedImport()
        {
            _importFileContextMenuHelper.ShowDialog(_selectedNode);
        }

        void SaveSelfAndChildren(TreeNode node, string outputDirectory, string rootPath, ref int fileCounter)
        {
            if (node.GetNodeType() == NodeType.Directory)
            {
                foreach (var item in node.Children)
                    SaveSelfAndChildren(item, outputDirectory, rootPath, ref fileCounter);
            }
            else
            {
                var nodeOriginalPath = node.GetFullPath();

                var nodePathWithoutRoot = nodeOriginalPath;
                if (rootPath.Length != 0)
                    nodePathWithoutRoot = nodeOriginalPath.Replace(rootPath, "");

                if (nodePathWithoutRoot.StartsWith("\\") == false)
                    nodePathWithoutRoot = "\\" + nodePathWithoutRoot;

                var fileOutputPath = outputDirectory + nodePathWithoutRoot;

                var fileOutputDir = Path.GetDirectoryName(fileOutputPath);
                DirectoryHelper.EnsureCreated(fileOutputDir);

                var packFile = node.Item;
                var bytes = packFile.DataSource.ReadData();

                File.WriteAllBytes(fileOutputPath, bytes);

                fileCounter++;
            }
        }

        void OpenPackFileUsing(string applicationPath, PackFile packFile)
        {
            if (File.Exists(applicationPath) == false)
            {
                MessageBox.Show($"Application {applicationPath} does not exist");
                return;
            }

            var tempFolder = Path.GetTempPath();
            var fileName = string.Format(@"{0}_", DateTime.Now.Ticks) + packFile.Name;

            var path = tempFolder + "\\" + fileName;
            var bytes = packFile.DataSource.ReadData();
            File.WriteAllBytes(path, bytes);

            Process.Start(applicationPath, $"\"{path}\"");
        }

        void ExpandAllRecursive(TreeNode node)
        {
            node.IsNodeExpanded = true;
            foreach (var child in node.Children)
                ExpandAllRecursive(child);
        }

        void CollapsAllRecursive(TreeNode node)
        {
            node.IsNodeExpanded = false;
            foreach (var child in node.Children)
                CollapsAllRecursive(child);
        }

        public abstract void Create(TreeNode node);

        protected ContextMenuItem Additem(ContextItems type, ContextMenuItem parent)
        {
            var item = GetItem(type);
            parent.ContextMenu.Add(item);
            return item;
        }

        protected ContextMenuItem Additem(ContextItems type, ObservableCollection<ContextMenuItem> parent)
        {
            var item = GetItem(type);
            parent.Add(item);
            return item;
        }

        protected void AddSeperator(ObservableCollection<ContextMenuItem> parent)
        {
            parent.Add(null);
        }

        ContextMenuItem GetItem(ContextItems type)
        {
            switch (type)
            {
                case ContextItems.Add:
                    return new ContextMenuItem() { Name = "Add" };
                case ContextItems.Import:
                    return new ContextMenuItem() { Name = "Import" };
                case ContextItems.AdvancedImport:
                    return new ContextMenuItem() { Name = "Advanced Import", Command = AdvancedImport };
                case ContextItems.Create:
                    return new ContextMenuItem() { Name = "Create" };
                case ContextItems.AddFiles:
                    return new ContextMenuItem() { Name = "Add file", Command = AddFilesCommand }; ;
                case ContextItems.AddDirectory:
                    return new ContextMenuItem() { Name = "Add directory", Command = AddFilesFromDirectory };
                case ContextItems.CopyToEditablePack:
                    return new ContextMenuItem() { Name = "Copy to Editable pack", Command = CopyToEditablePackCommand }; ;
                case ContextItems.Duplicate:
                    return new ContextMenuItem() { Name = "Duplicate", Command = DuplicateCommand }; ;
                case ContextItems.CreateFolder:
                    return new ContextMenuItem() { Name = "Create Folder", Command = CreateFolderCommand }; ;
                case ContextItems.Expand:
                    return new ContextMenuItem() { Name = "Expand (Ctrl + double click)", Command = ExpandAllChildrenCommand }; ;
                case ContextItems.Collapse:
                    return new ContextMenuItem() { Name = "Collapse", Command = CollapseAllChildrenCommand }; ;
                case ContextItems.CopyFullPath:
                    return new ContextMenuItem() { Name = "Copy full path", Command = CopyNodePathCommand };
                case ContextItems.ExportToFolder:
                    return new ContextMenuItem() { Name = "Export to disk", Command = ExportToFolderCommand };
                case ContextItems.AdvancedExport:
                    return new ContextMenuItem() { Name = "Advanced Export", Command = AdvancedExportToFolderCommand };
                case ContextItems.Rename:
                    return new ContextMenuItem() { Name = "Rename", Command = RenameNodeCommand }; ;
                case ContextItems.SetAsEditabelPack:
                    return new ContextMenuItem() { Name = "Set as Editable pack", Command = SetAsEditabelPackCommand };
                case ContextItems.Delete:
                    return new ContextMenuItem() { Name = "Delete", Command = DeleteCommand }; ;
                case ContextItems.Close:
                    return new ContextMenuItem() { Name = "Close", Command = CloseNodeCommand }; ;
                case ContextItems.Save:
                    return new ContextMenuItem() { Name = "Save", Command = SavePackFileCommand }; ;
                case ContextItems.SaveAs:
                    return new ContextMenuItem() { Name = "Save as", Command = SavePackFileAsCommand }; ;
                case ContextItems.Open:
                    return new ContextMenuItem() { Name = "Open", }; ;
                case ContextItems.OpenWithHxD:
                    return new ContextMenuItem() { Name = "HxD", Command = OpenPackFile_HxD_Command }; ;
                case ContextItems.OpenWithNodePadPluss:
                    return new ContextMenuItem() { Name = "Notepad++", Command = OpenPack_FileNotpadPluss_Command }; ;
            }

            throw new Exception($"Unknown ContextItemType  {type} ");
        }

        protected enum ContextItems
        {
            Add,
            Import,
            AdvancedImport,
            AddFiles,
            AddDirectory,
            CopyToEditablePack,
            Duplicate,
            CreateFolder,
            Create,

            Expand,
            Collapse,
            CopyFullPath,
            ExportToFolder,
            AdvancedExport,
            Rename,
            SetAsEditabelPack,

            Delete,
            Close,
            Save,
            SaveAs,

            Open,
            OpenWithHxD,
            OpenWithNodePadPluss,
        }
    }*/
}
