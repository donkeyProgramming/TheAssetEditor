using Common;
using CommonControls.PackFileBrowser;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using System.Windows.Input;

namespace AssetEditor.ViewModels
{
    public delegate void FileSelectedDelegate(IPackFile file);

    class FileTreeViewModel : PackFileBrowserViewModel
    {
        ILogger _logger = Logging.Create<FileTreeViewModel>();

        public event FileSelectedDelegate FileOpen;

        public ICommand RenameNodeCommand { get; set; }
        public ICommand AddEmptyFolderCommand { get; set; }
        public ICommand AddFilesFromDirectory { get; set; }
        
        public FileTreeViewModel(PackFileService packFileService) : base(packFileService)
        {
            RenameNodeCommand = new RelayCommand<TreeNode>(OnRenameNode);
            AddEmptyFolderCommand = new RelayCommand<TreeNode>(OnAddNewFolder);
            AddFilesFromDirectory = new RelayCommand<TreeNode>(OnAddFilesFromDirectory);
        }

        protected override void OnDoubleClick(TreeNode node)
        {
            if (node.Item.PackFileType() == PackFileType.Data)
                FileOpen?.Invoke(node.Item);
        }

        void OnRenameNode(TreeNode treeNode) 
        {
            TextInputWindow window = new TextInputWindow("Rename file", treeNode.Item.Name);
            if (window.ShowDialog() == true)
                _packFileService.RenameFile(treeNode.Item, window.TextValue);
        }

        void OnAddNewFolder(TreeNode node)
        {
            if (node.Item.PackFileType() != PackFileType.Data)
                _packFileService.AddEmptyFolder(node.Item as PackFileDirectory, "name");
        }

        void OnAddFilesFromDirectory(TreeNode node)
        {
           var dialog = new CommonOpenFileDialog();
           dialog.IsFolderPicker = true;
           if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
           {
               _logger.Here().Information($"Adding content of {dialog.FileName} to pack");
               _packFileService.AddFolderContent(node.Item, dialog.FileName);
           }
        }
    }
}
