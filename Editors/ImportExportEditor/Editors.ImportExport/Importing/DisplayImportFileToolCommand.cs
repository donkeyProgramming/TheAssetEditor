using System.Windows.Forms;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using TreeNode = Shared.Ui.BaseDialogs.PackFileTree.TreeNode;
using Shared.Core.PackFiles;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using System.Windows.Forms.Design;
using Shared.Core.Settings;
using Shared.Core.Misc;
using Editors.ImportExport.Importing.Presentation;

namespace Editors.ImportExport.Importing
{
    public class DisplayImportFileToolCommand : IAeCommand
    {
        private readonly IEnumerable<IImporterViewModel> _importerViewModels;
        private readonly IAbstractFormFactory<ImportWindow> _importWindowFactory;
        private IPackFileContainer _packFileContainer = null!;
        private string _packPath = string.Empty;

        public DisplayImportFileToolCommand(IAbstractFormFactory<ImportWindow> exportWindowFactory, IEnumerable<IImporterViewModel> exporterViewModels)
        {
            _importWindowFactory = exportWindowFactory;
            _importerViewModels = exporterViewModels;
        }

        public void Configure(IPackFileContainer packFileContainer, string packPath)
        {
            _packFileContainer = packFileContainer;
            _packPath = packPath;
        }

        public void Execute()
        {
            var fileExtentionFilters = GetFileDialogFilters();

            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Filter = fileExtentionFilters };
            if (openFileDialog.ShowDialog() != true)
                return;

            var diskFilePath = openFileDialog.FileName;

            var window = _importWindowFactory.Create();
            window.Initialize(_packFileContainer, _packPath, diskFilePath);
            window.ShowDialog();
        }

        /// <summary>
        /// Makes a filter string for the file dialog ("Word Documents|*.doc|Excel Worksheets|*.xls" )
        /// Searching all importer view models for their supported file extensions
        /// </summary>        
        private string GetFileDialogFilters()
        {
            var fileExtentionFilters = "";
            foreach (var importViewModel in _importerViewModels) // generate file dialog filters, of all support file formats
            {
                var tempFilter = $"*{String.Join(";*", importViewModel.InputExtensions)}";
                fileExtentionFilters += $"{importViewModel.DisplayName} ({tempFilter}) |{tempFilter}|";
            }

            fileExtentionFilters += "All files (*.*)|*.*";

            return fileExtentionFilters;
        }
    }
}
