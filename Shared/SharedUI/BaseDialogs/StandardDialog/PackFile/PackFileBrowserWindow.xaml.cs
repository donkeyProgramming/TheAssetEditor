using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu;
using Shared.Ui.BaseDialogs.PackFileTree;

namespace CommonControls.PackFileBrowser
{
    public partial class PackFileBrowserWindow : Window, IDisposable
    {
        public PackFileBrowserViewModel ViewModel { get; set; }

        public PackFile SelectedFile { get; set; }
        public TreeNode? SelectedNode { get; set; }
        public string SelectedFolder { get; set; }

        public PackFileBrowserWindow(PackFileTreeViewFactory packFileBrowserBuilder, List<string>? extensions, bool showCaFiles, bool showFoldersOnly, bool useEditablePackOnly)
        {
            ViewModel = packFileBrowserBuilder.Create(ContextMenuType.None, showCaFiles, showFoldersOnly, useEditablePackOnly);
            ViewModel.FileOpen += ViewModel_FileOpen;
            ViewModel.NodeSelected += ViewModel_NodeSelected;

            InitializeComponent();
            DataContext = this;
            PreviewKeyDown += HandleEsc;

            if (extensions != null)
                ViewModel.Filter.SetExtensions(extensions);
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

        private void ViewModel_NodeSelected(TreeNode node)
        {
            if (node.NodeType == NodeType.Directory)
                SelectedFolder = node.Name;
                if (DialogResult != true)
                    DialogResult = true;
                    Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedFile = ViewModel.SelectedItem?.Item;
            SelectedNode = ViewModel.SelectedItem?.NodeType == NodeType.Directory ? ViewModel.SelectedItem : null;
            DialogResult = true;
            Close();
        }

        public void Dispose()
        {
            PreviewKeyDown -= HandleEsc;
            ViewModel.FileOpen -= ViewModel_FileOpen;
            ViewModel.NodeSelected -= ViewModel_NodeSelected;
            ViewModel.Dispose();
            ViewModel = null;
            DataContext = null;
        }
    }
}
