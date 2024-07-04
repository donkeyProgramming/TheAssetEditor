using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.DataTemplates;

namespace Editors.ImportExport.Exporting.Presentation.RmvToGltf
{
    internal class RmvToGltfExporterViewModel : IExporterViewModel, IViewProvider
    {
        private readonly RmvToGltfExporter _exporter;

        public Type ViewType => typeof(RmvToGltfExporterView);
        public string DisplayName => "Rmv_to_Gltf";
        public string OutputExtension => ".glft";

        public RmvToGltfExporterViewModel(RmvToGltfExporter exporter)
        {
            _exporter = exporter;
        }

        public ExportSupportEnum CanExportFile(PackFile file) => _exporter.CanExportFile(file);

        public void Execute(string outputPath, bool generateImporter)
        {
            _exporter.Export(outputPath);
        }
    }
}
