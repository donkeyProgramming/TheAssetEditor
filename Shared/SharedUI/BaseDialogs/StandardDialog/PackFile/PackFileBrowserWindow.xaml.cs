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
        public PackFile SelectedFile { get; set; }
        public PackFileBrowserViewModel ViewModel { get; set; }

        public PackFileBrowserWindow(PackFileTreeViewFactory packFileBrowserBuilder) => Create(packFileBrowserBuilder);

        public PackFileBrowserWindow(PackFileTreeViewFactory packFileBrowserBuilder, List<string> extentions)
        {
            Create(packFileBrowserBuilder);
            ViewModel.Filter.SetExtentions(extentions);
        }

        void Create(PackFileTreeViewFactory packFileBrowserBuilder)
        {
            ViewModel = packFileBrowserBuilder.Create(ContextMenuType.None, true);
            ViewModel.FileOpen += ViewModel_FileOpen;
            ViewModel.Filter.AutoExapandResultsAfterLimitedCount = 50;
            
            InitializeComponent();
            DataContext = this;
            PreviewKeyDown += HandleEsc;
        }

        public new bool ShowDialog() => (this as Window).ShowDialog() == true && SelectedFile != null;

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
