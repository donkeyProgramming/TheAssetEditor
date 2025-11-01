using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;

namespace Shared.Ui.BaseDialogs.StandardDialog.PackFile
{
    public partial class PackFileBrowserWindow : Window, IDisposable
    {
        public Core.PackFiles.Models.PackFile SelectedFile { get; set; }
        public string SelectedFolder { get; set; }

        public PackFileBrowserViewModel ViewModel { get; set; }

        public PackFileBrowserWindow(PackFileTreeViewFactory packFileBrowserBuilder, List<string>? extensions, bool showCaFiles, bool showFoldersOnly)
        {
            Create(packFileBrowserBuilder, showCaFiles, showFoldersOnly);

            if (extensions != null)
                ViewModel.Filter.SetExtensions(extensions);
        }

        void Create(PackFileTreeViewFactory packFileBrowserBuilder, bool showCaFiles, bool showFoldersOnly)
        {
            ViewModel = packFileBrowserBuilder.Create(ContextMenuType.None, showCaFiles, showFoldersOnly);
            ViewModel.FileOpen += ViewModel_FileOpen;
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

        private void ViewModel_FileOpen(Core.PackFiles.Models.PackFile file)
        {
            SelectedFile = file;
            if (DialogResult != true)
                DialogResult = true;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedFile = ViewModel.SelectedItem?.Item;

            if (ViewModel.SelectedItem?.NodeType == NodeType.Directory)
                SelectedFolder = GetFolderPath(ViewModel.SelectedItem, ViewModel.SelectedItem?.Name);

            DialogResult = true;
            Close();
        }

        private static string GetFolderPath(TreeNode node, string folderPath)
        {
            if (node.Parent?.NodeType == NodeType.Root)
                return folderPath;
            else
            {
                folderPath = $"{node.Parent.Name}\\{folderPath}";
                return GetFolderPath(node.Parent, folderPath);
            }
        }

        public void Dispose()
        {
            PreviewKeyDown -= HandleEsc;
            ViewModel.FileOpen -= ViewModel_FileOpen;
            ViewModel.Dispose();
            ViewModel = null;
            DataContext = null;
        }
    }
}
