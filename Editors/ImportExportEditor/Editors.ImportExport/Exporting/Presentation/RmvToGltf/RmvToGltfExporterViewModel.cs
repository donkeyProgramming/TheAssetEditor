using CommunityToolkit.Mvvm.ComponentModel;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.DataTemplates;

namespace Editors.ImportExport.Exporting.Presentation.RmvToGltf
{
    internal partial class RmvToGltfExporterViewModel : ObservableObject, IExporterViewModel, IViewProvider<RmvToGltfExporterView>
    {
        private readonly RmvToGltfExporter _exporter;

        public string DisplayName => "Rmv_to_Gltf";
        public string OutputExtension => ".gltf";

        [ObservableProperty] bool _exportTextures = true;
        [ObservableProperty] bool _convertMaterialTextureToBlender = true;
        [ObservableProperty] bool _convertNormalTextureToBlue = true;
        [ObservableProperty] bool _exportAnimations = true;

        public RmvToGltfExporterViewModel(RmvToGltfExporter exporter)
        {
            _exporter = exporter;
        }

        public ExportSupportEnum CanExportFile(PackFile file) => _exporter.CanExportFile(file);

        public void Execute(PackFile exportSource, string outputPath, bool generateImporter)
        {
            var settings = new RmvToGltfExporterSettings(exportSource, [], outputPath, ConvertMaterialTextureToBlender, ConvertNormalTextureToBlue, ExportAnimations, true);
            _exporter.Export(settings);
        }
    }
}
