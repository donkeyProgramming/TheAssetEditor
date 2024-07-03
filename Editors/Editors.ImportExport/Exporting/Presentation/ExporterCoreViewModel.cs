using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.ImportExport.Exporting.Exporters;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Presentation
{
    public partial class ExporterCoreViewModel : ObservableObject
    {
        private readonly IEnumerable<IExporterViewModel> _exporterViewModels;
        string _fileName = string.Empty;

        [ObservableProperty] IExporterViewModel? _selectedExporterViewModel;
        [ObservableProperty] ObservableCollection<IExporterViewModel> _possibleExporters = [];
        [ObservableProperty] IExporterViewModel? _selectedExporter;
        [ObservableProperty] string _systemPath = "C:\\myfile.dds";
        [ObservableProperty] bool _createImportProject = true;

        public ExporterCoreViewModel(IEnumerable<IExporterViewModel> exporterViewModels)
        {
            _exporterViewModels = exporterViewModels;
        }

        public void Initialize(PackFile packFile)
        {
            _fileName = packFile.Name;
            foreach (var viewModel in _exporterViewModels)
            {
                var supported = viewModel.CanExportFile(packFile);
                if (supported == ExportSupportEnum.NotSupported)
                    continue;

                PossibleExporters.Add(viewModel);
                if(supported == ExportSupportEnum.HighPriority)
                    SelectedExporter = viewModel;
            }

            if (SelectedExporter == null)
                SelectedExporter = PossibleExporters.FirstOrDefault();
        }

        public void Export() => SelectedExporter!.Execute(SystemPath, true);

        [RelayCommand]
        public void BrowsePathCommand()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Path.GetFileNameWithoutExtension(_fileName),
                DefaultExt = SelectedExporter!.OutputExtension,
                Filter = $"File ({SelectedExporter!.OutputExtension})|*{SelectedExporter!.OutputExtension}"
            };

            if (dlg.ShowDialog() == true)
                SystemPath = dlg.FileName;
        }
        
    }
}
