using System.IO;
using System.Windows;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Presentation
{
    /// <summary>
    /// Interaction logic for ExportWindow.xaml
    /// </summary>
    public partial class ExportWindow : Window
    {
        private readonly ExporterCoreViewModel _viewModel;

        public ExportWindow(ExporterCoreViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        internal void Initialize(PackFile packFile)
        {
            _viewModel.Initialize(packFile);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PathValidator.IsValid(_viewModel.SystemPath))
            {
                MessageBox.Show("Invalid or empty path", "Error");
                return;
            }

            _viewModel.Export();
            Close();
        }
    }

    // TODO: should be moved to a shared library
    public static class PathValidator
    {
        public static bool IsValid(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            if (!IsValidFileName(fileName))
            {
                return false;
            }

            if (!IsValidFolder(fileName))
            {
                return false;
            }

            return true;
        }

        private static bool IsValidFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var fileNamOnly = Path.GetFileName(fileName);
            foreach (char c in fileNamOnly)
            {
                if (Array.Exists(invalidChars, invalidChar => invalidChar == c))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidFolder(string fileName)
        {
            char[] invalidChars = Path.GetInvalidPathChars();
            var folder = Path.GetDirectoryName(fileName);
            foreach (char c in folder)
            {
                if (Array.Exists(invalidChars, invalidChar => invalidChar == c))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
