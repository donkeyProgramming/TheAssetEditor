using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Misc;
using GameWorld.Core.Services;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.DataTemplates;

namespace Editors.ImportExport.Exporting.Presentation.RmvToGltf
{
    public sealed class GltfAnimationSelectionItem : ObservableObject
    {
        private bool _isSelected;

        public GltfAnimationSelectionItem(AnimationReference reference)
        {
            Reference = reference;
        }

        public AnimationReference Reference { get; }
        public string DisplayName => Reference.AnimationFile;
        public string ContainerName => Reference.Container?.Name ?? string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    internal partial class RmvToGltfExporterViewModel : ObservableObject, IExporterViewModel, IViewProvider<RmvToGltfExporterView>
    {
        private readonly IRmvToGltfExporter _exporter;
        private readonly IPackFileService _packFileService;
        private readonly IGltfAnimationCatalogResolver _animationCatalogResolver;
        private PackFile? _initializedSource;
        private string _catalogResolutionStatus = string.Empty;

        public ObservableCollection<GltfAnimationSelectionItem> AnimationOptions { get; } = [];

        public string DisplayName => "Rmv_to_Gltf";
        public string OutputExtension => ".gltf";
        public string OutputFilter => "glTF (*.gltf)|*.gltf|Binary glTF (*.glb)|*.glb";

        [ObservableProperty] bool _exportTextures = true;
        [ObservableProperty] bool _convertMaterialTextureToBlender = true;
        [ObservableProperty] bool _convertNormalTextureToBlue = true;
        [ObservableProperty] bool _exportAnimations = true;
        [ObservableProperty] private string _animationSearch = string.Empty;
        [ObservableProperty] private string _skeletonStatus = "No source model selected.";
        [ObservableProperty] private string _animationStatus = "No animations available.";
        [ObservableProperty] private string _resolutionStatus = string.Empty;

        public string? SkeletonName { get; private set; }
        public int SelectedAnimationCount => AnimationOptions.Count(x => x.IsSelected);
        public bool HasAnimationOptions => AnimationOptions.Count != 0;

        public IEnumerable<GltfAnimationSelectionItem> FilteredAnimationOptions
            => string.IsNullOrWhiteSpace(AnimationSearch)
                ? AnimationOptions
                : AnimationOptions.Where(x => x.DisplayName.Contains(AnimationSearch, StringComparison.OrdinalIgnoreCase));

        public RmvToGltfExporterViewModel(
            IRmvToGltfExporter exporter,
            IPackFileService packFileService,
            IGltfAnimationCatalogResolver animationCatalogResolver)
        {
            _exporter = exporter;
            _packFileService = packFileService;
            _animationCatalogResolver = animationCatalogResolver;
        }

        public ExportSupportEnum CanExportFile(PackFile file) => _exporter.CanExportFile(file);

        public void Initialize(PackFile exportSource)
        {
            ClearAnimationOptions();
            _initializedSource = exportSource;
            SkeletonName = null;
            OnPropertyChanged(nameof(SkeletonName));
            _catalogResolutionStatus = string.Empty;
            ResolutionStatus = _catalogResolutionStatus;
            SkeletonStatus = "Resolving skeleton...";
            AnimationStatus = "Resolving animations...";

            GltfAnimationCatalog catalog;
            try
            {
                catalog = _animationCatalogResolver.Resolve(exportSource);
            }
            catch (Exception exception)
            {
                SkeletonStatus = "No skeleton available.";
                AnimationStatus = "No animations available.";
                _catalogResolutionStatus = $"Unable to resolve export source: {exception.Message}";
                ResolutionStatus = _catalogResolutionStatus;
                return;
            }

            if (catalog.Diagnostics.Count != 0)
                _catalogResolutionStatus = string.Join(Environment.NewLine, catalog.Diagnostics);
            ResolutionStatus = _catalogResolutionStatus;

            if (string.IsNullOrWhiteSpace(catalog.SkeletonName))
            {
                SkeletonStatus = "No skeleton found for this model.";
                AnimationStatus = "No animations available.";
                return;
            }

            SkeletonName = catalog.SkeletonName;
            OnPropertyChanged(nameof(SkeletonName));

            if (catalog.HasSkeletonFile == false)
            {
                SkeletonStatus = $"Skeleton '{catalog.SkeletonName}' was not found in the loaded packs.";
                AnimationStatus = "No animations available.";
                return;
            }

            SkeletonStatus = $"Skeleton: {catalog.SkeletonName}";
            foreach (var animationReference in catalog.Animations
                .OrderBy(x => x.AnimationFile, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Container?.Name, StringComparer.OrdinalIgnoreCase))
            {
                var item = new GltfAnimationSelectionItem(animationReference);
                item.PropertyChanged += AnimationItemPropertyChanged;
                AnimationOptions.Add(item);
            }
            OnPropertyChanged(nameof(FilteredAnimationOptions));

            // Match the Kitbash preview's useful default without silently
            // exporting every animation in a large animation catalog.
            var defaultIdle = AnimationOptions.FirstOrDefault(x =>
                x.DisplayName.Contains("stand_idle", StringComparison.OrdinalIgnoreCase));
            if (defaultIdle != null)
                defaultIdle.IsSelected = true;

            OnPropertyChanged(nameof(HasAnimationOptions));
            RefreshAnimationStatus();
        }

        [RelayCommand]
        private void SelectAllAnimations()
        {
            foreach (var item in AnimationOptions)
                item.IsSelected = true;
            RefreshAnimationStatus();
        }

        [RelayCommand]
        private void SelectNoneAnimations()
        {
            foreach (var item in AnimationOptions)
                item.IsSelected = false;
            RefreshAnimationStatus();
        }

        public void Execute(PackFile exportSource, string outputPath, bool generateImporter)
        {
            if (!ReferenceEquals(_initializedSource, exportSource))
                Initialize(exportSource);

            ResolutionStatus = _catalogResolutionStatus;
            var unresolvedCount = 0;
            var selectedAnimationFiles = ExportAnimations
                ? ResolveSelectedAnimationFiles(out unresolvedCount)
                : new List<PackFile>();

            if (ExportAnimations && unresolvedCount > 0)
            {
                ResolutionStatus = AppendStatus(
                    ResolutionStatus,
                    $"Skipped {unresolvedCount} selected animation(s) that could not be resolved from the loaded packs.");
            }

            var settings = new RmvToGltfExporterSettings(
                exportSource,
                selectedAnimationFiles,
                outputPath,
                ExportTextures,
                ConvertMaterialTextureToBlender,
                ConvertNormalTextureToBlue,
                ExportAnimations,
                true);
            _exporter.Export(settings);
        }

        private List<PackFile> ResolveSelectedAnimationFiles(out int unresolvedCount)
        {
            unresolvedCount = 0;
            var output = new List<PackFile>();
            var seen = new HashSet<PackFile>();

            foreach (var item in AnimationOptions.Where(x => x.IsSelected))
            {
                PackFile? packFile;
                try
                {
                    packFile = _packFileService.FindFile(item.Reference.AnimationFile, item.Reference.Container);
                }
                catch
                {
                    packFile = null;
                }

                if (packFile == null)
                {
                    unresolvedCount++;
                    continue;
                }

                if (seen.Add(packFile))
                    output.Add(packFile);
            }

            return output;
        }

        private void ClearAnimationOptions()
        {
            foreach (var item in AnimationOptions)
                item.PropertyChanged -= AnimationItemPropertyChanged;
            AnimationOptions.Clear();
            OnPropertyChanged(nameof(HasAnimationOptions));
            OnPropertyChanged(nameof(FilteredAnimationOptions));
            RefreshAnimationStatus();
        }

        private void AnimationItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(GltfAnimationSelectionItem.IsSelected))
                return;

            OnPropertyChanged(nameof(SelectedAnimationCount));
            RefreshAnimationStatus();
        }

        private void RefreshAnimationStatus()
        {
            AnimationStatus = AnimationOptions.Count == 0
                ? "No animations available."
                : $"{SelectedAnimationCount} of {AnimationOptions.Count} animations selected.";
        }

        private static string AppendStatus(string current, string addition)
            => string.IsNullOrWhiteSpace(current) ? addition : current + Environment.NewLine + addition;

        partial void OnAnimationSearchChanged(string value)
            => OnPropertyChanged(nameof(FilteredAnimationOptions));
    }
}
