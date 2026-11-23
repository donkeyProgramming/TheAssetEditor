using CommunityToolkit.Mvvm.ComponentModel;
using Editors.ImportExport.Importing;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using Editors.ImportExport.Importing.Presentation;
using Editors.ImportExport.Importing.Presentation.RmvToGltf;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.Ui.Common.DataTemplates;

namespace Editors.ImportImport.Importing.Presentation.RmvToGltf
{
    public partial class RmvToGltfImporterViewModel : ObservableObject, IImporterViewModel, IViewProvider<RmvToGltfImporterView>
    {
        private readonly GltfImporter _importer;

        public string DisplayName => "Gltf Importer";
        public string OutputExtension => ".rigid_model_v2";
        public string[] InputExtensions => new string[] { ".gltf", ".glb" };
        public bool SupportsCancellation => true;

        [ObservableProperty] bool _importMeshes = true;
        [ObservableProperty] bool _importMaterials = true;
        [ObservableProperty] bool _convertFromBlenderMaterialMap = true;
        [ObservableProperty] bool _convertNormalTextureToOrange = true;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(CanEditAnimationKeysPerSecond))] bool _importAnimations = true;
        [ObservableProperty] bool _autoScaleHumanoid = true;
        [ObservableProperty] GltfSourceForwardDirection _sourceForwardDirection = GltfSourceForwardDirection.PositiveZ;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(CanEditAnimationKeysPerSecond))] bool _autoDetectAnimationKeysPerSecond = true;
        [ObservableProperty] float _animationKeysPerSecond = 20.0f;
        [ObservableProperty] string _newSkeletonName = "";

        public bool CanEditAnimationKeysPerSecond => ImportAnimations && !AutoDetectAnimationKeysPerSecond;

        // The axis names are the same in every language, so they aren't localised.
        public IReadOnlyList<KeyValuePair<string, GltfSourceForwardDirection>> SourceForwardDirections { get; } =
        [
            new("+Z (standard glTF)", GltfSourceForwardDirection.PositiveZ),
            new("+X (Unreal/PSK)", GltfSourceForwardDirection.PositiveX),
            new("-X", GltfSourceForwardDirection.NegativeX),
            new("-Z", GltfSourceForwardDirection.NegativeZ),
        ];

        public RmvToGltfImporterViewModel(GltfImporter importer)
        {
            _importer = importer;
        }

        public ImportSupportEnum CanImportFile(PackFile file) => _importer.CanImportFile(file);

        public void Initialize(PackFile inputFile) => NewSkeletonName = GltfSkeletonNameReader.GetDefaultName(inputFile.Name);

        public ImportResult Execute(PackFile importSource, string outputPath, IPackFileContainer packFileContainer, GameTypeEnum gameType, IProgress<ImportProgress>? progress, CancellationToken cancellationToken)
        {
            if (!float.IsFinite(AnimationKeysPerSecond) || AnimationKeysPerSecond <= 0)
                return ImportResult.Failure(["The animation key samples per second must be greater than 0."]);

            var settings = new GltfImporterSettings(
                InputGltfFile: importSource.Name,
                DestinationPackPath: outputPath,
                DestinationPackFileContainer: packFileContainer,
                SelectedGame: gameType,
                ImportMeshes: ImportMeshes,
                ImportMaterials: ImportMaterials,
                ConvertMaterialFromBlenderType: ConvertFromBlenderMaterialMap,
                ConvertNormalTextureFromBlueToOrangeType: ConvertNormalTextureToOrange,
                ImportAnimations: ImportAnimations,
                AnimationKeysPerSecond: AnimationKeysPerSecond,
                AutoDetectAnimationKeysPerSecond: AutoDetectAnimationKeysPerSecond,
                NewSkeletonName: NewSkeletonName,
                AutoScaleHumanoid: AutoScaleHumanoid,
                SourceForwardDirection: SourceForwardDirection);

            return _importer.Import(settings, progress, cancellationToken);
        }
    }
}
