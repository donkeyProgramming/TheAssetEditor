using Common;
using CommonControls.Services;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CommonControls.PackFileBrowser
{
    /// <summary>
    /// Interaction logic for PackFileBrowserWindow.xaml
    /// </summary>
    public partial class PackFileBrowserWindow : Window, IDisposable
    {
        public PackFile SelectedFile { get; set; }
        public PackFileBrowserViewModel ViewModel { get; set; }
        public PackFileBrowserWindow(PackFileService packfileService)
        {
            ViewModel = new PackFileBrowserViewModel(packfileService);
            ViewModel.FileOpen += ViewModel_FileOpen;
            InitializeComponent();
            DataContext = this;
        }

        private void ViewModel_FileOpen(IPackFile file)
        {
            SelectedFile = file as PackFile;
            if(DialogResult != true)
                DialogResult = true;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedFile = ViewModel.SelectedItem?.Item as PackFile;
            DialogResult = true;
            Close();
        }

        public void Dispose()
        {
            ViewModel.Dispose();
            ViewModel = null;
            DataContext = null;
        }
    }
}
