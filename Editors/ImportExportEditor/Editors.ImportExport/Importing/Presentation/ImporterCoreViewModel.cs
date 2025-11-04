using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Editors.ImportExport.Importing.Presentation
{
    // Importer
    // --------------------------
    //  Checkbox | Type | Path | Last changed | Need refresh    | Remove Button |
    // --------------------------
    //  Checkbox | Type | Path | Last changed | Need refresh    | Remove Button |
    // --------------------------
    // | Update all Button |
    // => SHow status window after done with OK | Errors


    public partial class ImporterCoreViewModel : ObservableObject
    {
        private readonly ApplicationSettingsService _applicationSettings;

        private readonly IEnumerable<IImporterViewModel> _exporterViewModels;
        PackFile? _inputFile;
        PackFileContainer? _destPackFileContainer;
        string _packPath = "";

        [ObservableProperty] IImporterViewModel? _selectedImporterViewModel;
        [ObservableProperty] ObservableCollection<IImporterViewModel> _possibleImporters = [];
        [ObservableProperty] IImporterViewModel? _selectedImporter;
        [ObservableProperty] string _systemPath = "";
        [ObservableProperty] bool _createImportProject = true;

        public ImporterCoreViewModel(IEnumerable<IImporterViewModel> exporterViewModels, ApplicationSettingsService applicationSettings)
        {
            _exporterViewModels = exporterViewModels;
            _applicationSettings = applicationSettings;
        }

        public void Initialize(PackFileContainer packFile, string packPath, string diskFile)
        {
            _destPackFileContainer = packFile;
            _packPath = packPath;
            SystemPath = diskFile;



            _inputFile = new PackFile(SystemPath, new FileSystemSource(SystemPath));
            FindImporter();
        }

        public void FindImporter()        
        {            
            

            if(_inputFile == null)
                throw new ArgumentNullException(nameof(_inputFile), "Fatal Eroor, cannot be null");

            foreach (var viewModel in _exporterViewModels)
            {
                var supported = viewModel.CanImportFile(_inputFile);
                if (supported == ImportSupportEnum.NotSupported)
                    continue;

                PossibleImporters.Add(viewModel);                

                if (supported == ImportSupportEnum.HighPriority)
                    SelectedImporter = viewModel;
            }

            if (SelectedImporter == null)
                SelectedImporter = PossibleImporters.First();
        }

        public void Import() => SelectedImporter!.Execute(_inputFile, _packPath, _destPackFileContainer, _applicationSettings.CurrentSettings.CurrentGame);

        [RelayCommand]
        public void BrowsePathCommand()
        {
            int i = 10;
            i = i + 10;
        }
    }
}
