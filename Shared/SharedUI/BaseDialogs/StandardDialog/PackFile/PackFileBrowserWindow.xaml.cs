using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu;

namespace CommonControls.PackFileBrowser
{
    public partial class PackFileBrowserWindow : Window, IDisposable
    {
        public PackFile SelectedFile { get; set; }
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedFile = ViewModel.SelectedItem?.Item;

            if (ViewModel.SelectedItem?.NodeType == NodeType.Directory)
                SelectedFolder = ViewModel.SelectedItem?.Name;

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
    }
}
