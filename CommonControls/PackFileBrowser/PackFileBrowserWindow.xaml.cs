using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;

namespace CommonControls.PackFileBrowser
{
    public partial class PackFileBrowserWindow : Window, IDisposable
    {
        public PackFile SelectedFile { get; set; }
        public PackFileBrowserViewModel ViewModel { get; set; }

        public PackFileBrowserWindow(PackFileService packfileService) => Create(packfileService);

        public PackFileBrowserWindow(PackFileService packfileService, string[] extentions)
        {
            Create(packfileService);
            ViewModel.Filter.SetExtentions(extentions.ToList());
        }

        void Create(PackFileService packfileService)
        {
            ViewModel = new PackFileBrowserViewModel(packfileService);
            ViewModel.ContextMenu = new OpenFileContexMenuHandler(packfileService);
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
