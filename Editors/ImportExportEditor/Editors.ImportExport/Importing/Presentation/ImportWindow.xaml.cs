using System.ComponentModel;
using System.Windows;
using Editors.ImportExport.Exporting.Presentation;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Editors.ImportExport.Importing.Presentation
{
    public partial class ImportWindow : Window
    {
        private readonly ImporterCoreViewModel _viewModel;
        private readonly IStandardDialogs _standardDialogs;

        public ImportWindow(ImporterCoreViewModel viewModel, IStandardDialogs standardDialogs)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _standardDialogs = standardDialogs;
            DataContext = _viewModel;
        }

        internal void Initialize(IPackFileContainer packFileContainer, string packPath, string diskFile)
        {
            _viewModel.Initialize(packFileContainer, packPath, diskFile);
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsOperationActive)
                return;

            if (!PathValidator.IsValid(_viewModel.SystemPath))
            {
                _standardDialogs.ShowDialogBox("Invalid or empty path", "Error");
                return;
            }

            ImportResult? result;
            try
            {
                result = await _viewModel.ImportAsync();
            }
            catch (Exception ex)
            {
                _standardDialogs.ShowExceptionWindow(ex, "The import failed.");
                return;
            }

            if (result == null)
                return;

            var message = ImporterCoreViewModel.BuildResultMessage(result);
            if (result.Succeeded)
            {
                _standardDialogs.ShowDialogBox(message, "Import finished");
                Close();
            }
            else if (result.Exception != null)
            {
                _standardDialogs.ShowExceptionWindow(result.Exception, message);
            }
            else
            {
                _standardDialogs.ShowDialogBox(message, "Import failed");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // The import writes to the pack at the end, so the window stays until it has finished or been cancelled.
            if (_viewModel.IsOperationActive)
                e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
