using System.Windows;
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
            _viewModel.Export();
            Close();
        }
    }
}
