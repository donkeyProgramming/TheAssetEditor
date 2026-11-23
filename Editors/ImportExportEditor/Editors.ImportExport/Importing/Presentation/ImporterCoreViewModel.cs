using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Settings;

namespace Editors.ImportExport.Importing.Presentation
{
    public partial class ImporterCoreViewModel : ObservableObject
    {
        private readonly ApplicationSettingsService _applicationSettings;
        private readonly IEnumerable<IImporterViewModel> _importerViewModels;
        private CancellationTokenSource? _cancellation;
        PackFile? _inputFile;
        IPackFileContainer? _destPackFileContainer;
        string _packPath = "";

        [ObservableProperty] ObservableCollection<IImporterViewModel> _possibleImporters = [];
        [ObservableProperty] IImporterViewModel? _selectedImporter;
        [ObservableProperty] string _systemPath = "";
        [ObservableProperty] bool _createImportProject = true;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(CancelImportCommand))] bool _isOperationActive;
        [ObservableProperty] string _progressStatus = "";
        [ObservableProperty] string _progressDetail = "";
        [ObservableProperty] double _progressValue;
        [ObservableProperty] double _progressMaximum = 1;
        [ObservableProperty] bool _isProgressIndeterminate = true;

        public ImporterCoreViewModel(IEnumerable<IImporterViewModel> importerViewModels, ApplicationSettingsService applicationSettings)
        {
            _importerViewModels = importerViewModels;
            _applicationSettings = applicationSettings;
        }

        public void Initialize(IPackFileContainer packFile, string packPath, string diskFile)
        {
            _destPackFileContainer = packFile;
            _packPath = packPath;
            SystemPath = diskFile;

            _inputFile = new PackFile(SystemPath, new FileSystemSource(SystemPath));
            FindImporter();
        }

        public void FindImporter()
        {
            if (_inputFile == null)
                throw new ArgumentNullException(nameof(_inputFile), "Fatal Error, cannot be null");

            PossibleImporters.Clear();
            SelectedImporter = null;

            foreach (var viewModel in _importerViewModels)
            {
                var supported = viewModel.CanImportFile(_inputFile);
                if (supported == ImportSupportEnum.NotSupported)
                    continue;

                viewModel.Initialize(_inputFile);
                PossibleImporters.Add(viewModel);

                if (supported == ImportSupportEnum.HighPriority)
                    SelectedImporter = viewModel;
            }

            if (SelectedImporter == null && PossibleImporters.Count > 0)
                SelectedImporter = PossibleImporters.First();
        }

        /// <summary>
        /// Runs the selected importer on a background thread. Returns null when the user cancelled.
        /// </summary>
        public async Task<ImportResult?> ImportAsync()
        {
            if (SelectedImporter == null || _inputFile == null || _destPackFileContainer == null)
                throw new InvalidOperationException("No importer is available for this file.");

            var importer = SelectedImporter;
            var inputFile = _inputFile;
            var container = _destPackFileContainer;
            var game = _applicationSettings.CurrentSettings.CurrentGame;
            var progress = new Progress<ImportProgress>(ApplyProgress);

            ProgressStatus = "Importing...";
            ProgressDetail = "";
            ProgressValue = 0;
            ProgressMaximum = 1;
            IsProgressIndeterminate = true;
            _cancellation = importer.SupportsCancellation ? new CancellationTokenSource() : null;
            IsOperationActive = true;
            try
            {
                var token = _cancellation?.Token ?? CancellationToken.None;
                return await Task.Run(() => importer.Execute(inputFile, _packPath, container, game, progress, token), token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            finally
            {
                IsOperationActive = false;
                _cancellation?.Dispose();
                _cancellation = null;
                CancelImportCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanCancelImport))]
        void CancelImport() => _cancellation?.Cancel();

        bool CanCancelImport() => IsOperationActive && _cancellation != null;

        private void ApplyProgress(ImportProgress progress)
        {
            ProgressStatus = progress.Status;
            ProgressDetail = progress.Detail ?? "";
            IsProgressIndeterminate = progress.Total <= 0;
            ProgressMaximum = Math.Max(1, progress.Total);
            ProgressValue = Math.Clamp(progress.Completed, 0, ProgressMaximum);
        }

        public static string BuildResultMessage(ImportResult result)
        {
            var sections = new List<string>();
            if (result.Succeeded)
                sections.Add(result.OutputPaths.Count == 0 ? "The import finished but wrote no files." : BuildSection("Files written:", result.OutputPaths));
            else
                sections.Add(BuildSection("The import failed:", result.Errors));

            if (result.Warnings.Count != 0)
                sections.Add(BuildSection("Warnings:", result.Warnings));

            if (result.MaterialSummary != null)
            {
                var materialItems = result.MaterialSummary.MaskedMaterials
                    .Select(item => $"Alpha-masked material \"{item.MaterialName}\": WH3 alpha test turned on, base colour cutoff {item.AlphaCutoff:0.###}.")
                    .Concat(result.MaterialSummary.SkippedSemantics.Select(item =>
                        $"Material \"{item.MaterialName}\": skipped the glTF {GetMaterialSemanticDisplayName(item.Semantic)} channel, which has no reliable game equivalent."))
                    .ToList();
                if (materialItems.Count != 0)
                    sections.Add(BuildSection("Materials:", materialItems));
            }

            if (result.HumanoidScale != null)
            {
                var scale = result.HumanoidScale;
                sections.Add(BuildSection("Automatic humanoid scaling:",
                [
                    scale.SourceHeight != null ? $"Source height: {scale.SourceHeight.Value:0.####}" : "Source height: not measured",
                    scale.ReferenceHeight != null ? $"humanoid01 reference height: {scale.ReferenceHeight.Value:0.####}" : "humanoid01 reference height: not measured",
                    $"Scale factor: {scale.ScaleFactor:0.######}",
                    scale.Reason,
                ]));
            }

            if (result.SourceForward != null)
                sections.Add(BuildSection("Source front direction:", [$"Used: {result.SourceForward.SourceDirection}", result.SourceForward.Conversion]));

            return string.Join(Environment.NewLine + Environment.NewLine, sections);
        }

        private static string GetMaterialSemanticDisplayName(string semantic) => semantic switch
        {
            "Emissive" => "emissive",
            "Occlusion" => "occlusion",
            _ => $"\"{semantic}\"",
        };

        private static string BuildSection(string heading, IReadOnlyList<string> items) =>
            heading + Environment.NewLine + string.Join(Environment.NewLine, items.Select(item => $"• {item}"));
    }
}
