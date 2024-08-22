using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileBrowser;

namespace CommonControls.PackFileBrowser
{
    public partial class PackFileBrowserWindow : Window, IDisposable
    {
        public PackFile SelectedFile { get; set; }
        public TreeNode SelectedFolder { get; set; }
        public PackFileBrowserViewModel ViewModel { get; set; }
        public bool AllowFolderSelection { get; set; }

        public PackFileBrowserWindow(PackFileService packfileService) => Create(packfileService);

        public PackFileBrowserWindow(PackFileService packfileService, string[] extentions)
        {
            Create(packfileService);
            ViewModel.Filter.SetExtentions(extentions.ToList());
        }

        public PackFileBrowserWindow(PackFileService packfileService, string[] extentions, bool allowFolderSelection)
        {
            AllowFolderSelection = allowFolderSelection;
            Create(packfileService);
            ViewModel.Filter.SetExtentions(extentions.ToList());
        }

        void Create(PackFileService packfileService)
        {
            ViewModel = new PackFileBrowserViewModel(packfileService, allowFolderSelection: AllowFolderSelection);
            ViewModel.ContextMenu = new OpenFileContexMenuHandler(packfileService);
            ViewModel.FileOpen += ViewModel_FileOpen;
            ViewModel.FolderSelected += ViewModel_FolderSelected;
            ViewModel.Filter.AutoExapandResultsAfterLimitedCount = 50;
            InitializeComponent();
            DataContext = this;

            PreviewKeyDown += HandleEsc;
        }

        public new bool ShowDialog() => (this as Window).ShowDialog() == true && (SelectedFile != null || SelectedFolder != null);

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void ViewModel_FileOpen(PackFile file)
        {
            SelectedFile = file;
            if (DialogResult != true)
                DialogResult = true;
            Close();
        }

        private void ViewModel_FolderSelected(TreeNode folder)
        {
            SelectedFolder = folder;
            if (DialogResult != true)
                DialogResult = true;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedFile = ViewModel.SelectedItem?.Item;
            SelectedFolder = ViewModel.SelectedItem?.NodeType == NodeType.Directory ? ViewModel.SelectedItem : null;
            DialogResult = true;
            Close();
        }

        public void Dispose()
        {
            PreviewKeyDown -= HandleEsc;
            ViewModel.FileOpen -= ViewModel_FileOpen;
            ViewModel.Dispose();
            ViewModel = null;
            DataContext = null;
        }

        public string SelectedPath
        {
            get
            {
                if (SelectedFile != null)
                {
                    var node = FindNodeForFile(SelectedFile);
                    return node?.GetFullPath() ?? string.Empty;
                }

                else if (SelectedFolder != null)
                    return SelectedFolder.GetFullPath();

                return string.Empty;
            }
        }

        private TreeNode FindNodeForFile(PackFile file)
        {
            foreach (var rootNode in ViewModel.Files)
            {
                var node = FindNodeInTree(rootNode, file);
                if (node != null)
                    return node;
            }
            return null;
        }

        private TreeNode FindNodeInTree(TreeNode node, PackFile file)
        {
            if (node.Item == file)
                return node;

            foreach (var child in node.Children)
            {
                var foundNode = FindNodeInTree(child, file);
                if (foundNode != null)
                    return foundNode;
            }

            return null;
        }
    }
}
