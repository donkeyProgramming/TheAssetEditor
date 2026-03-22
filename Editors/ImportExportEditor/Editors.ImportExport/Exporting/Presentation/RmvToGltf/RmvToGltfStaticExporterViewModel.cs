using CommunityToolkit.Mvvm.ComponentModel;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.DataTemplates;

namespace Editors.ImportExport.Exporting.Presentation.RmvToGltf
{
    public partial class RmvToGltfStaticExporterViewModel : ObservableObject, IExporterViewModel, IViewProvider<RmvToGltfStaticExporterView>
    {
        private readonly RmvToGltfStaticExporter _exporter;

        [ObservableProperty] bool _exportTextures = true;

        // Displacement map quality settings for 3D printing
        [ObservableProperty] int _displacementIterations = 10;
        [ObservableProperty] float _displacementContrast = 0.1f;  // Slight contrast boost for detail
        [ObservableProperty] float _displacementSharpness = 0.0f;  // Start with no extra sharpening
        [ObservableProperty] bool _export16BitDisplacement = true;
        [ObservableProperty] bool _useMultiScaleProcessing = false;  // Disable by default
        [ObservableProperty] bool _usePoissonReconstruction = false;  // Disable by default

        public string DisplayName => "GLTF for 3D Printing";
        public string OutputExtension => ".gltf";

        public RmvToGltfStaticExporterViewModel(RmvToGltfStaticExporter exporter)
        {
            _exporter = exporter;
        }

        public ExportSupportEnum CanExportFile(PackFile file)
        {
            return _exporter.CanExportFile(file);
        }

        public void Execute(PackFile exportSource, string outputPath, bool generateImporter)
        {
            var settings = new RmvToGltfExporterSettings(
                exportSource, 
                [], 
                outputPath, 
                ExportTextures, 
                false,  // ConvertMaterialTextureToBlender - not used for static export
                false,  // ConvertNormalTextureToBlue - not used, handled automatically
                false,  // ExportAnimations - static mesh has no animations
                true,   // MirrorMesh
                true,   // ExportDisplacementMaps - ENABLED for 3D printing!
                DisplacementIterations,
                DisplacementContrast,
                DisplacementSharpness,
                Export16BitDisplacement,
                UseMultiScaleProcessing,
                UsePoissonReconstruction
            );
            _exporter.Export(settings);
        }
    }
}
