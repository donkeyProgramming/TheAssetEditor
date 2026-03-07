using CommunityToolkit.Mvvm.ComponentModel;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Presentation.RmvToGltf
{
    public partial class RmvToGltfStaticExporterViewModel : ObservableObject, IExporterViewModel
    {
        private readonly RmvToGltfStaticExporter _exporter;

        [ObservableProperty] bool _exportTextures = true;
        [ObservableProperty] bool _convertMaterialTextureToBlender = false;
        [ObservableProperty] bool _convertNormalTextureToBlue = false;
        [ObservableProperty] bool _generateDisplacementMaps = true;

        public string DisplayName => "GLTF (Static Mesh)";
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
            var settings = new RmvToGltfExporterSettings(exportSource, [], outputPath, ExportTextures, ConvertMaterialTextureToBlender, ConvertNormalTextureToBlue, false, true);
            _exporter.Export(settings);
        }
    }
}
