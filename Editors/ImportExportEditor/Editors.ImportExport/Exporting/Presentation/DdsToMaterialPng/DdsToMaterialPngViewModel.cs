using CommunityToolkit.Mvvm.ComponentModel;
using Editors.ImportExport.Common;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.DataTemplates;

namespace Editors.ImportExport.Exporting.Presentation.DdsToMaterialPng
{
    internal partial class DdsToMaterialPngViewModel : ObservableObject, IExporterViewModel, IViewProvider<DdsToMaterialPngView>
    {
        public string DisplayName => "Dds_to_MaterialPng";
        public string OutputExtension => ".png";

        [ObservableProperty] bool _swapBlender = true;
        private readonly IDdsToMaterialPngExporter _exporter;

        public DdsToMaterialPngViewModel(IDdsToMaterialPngExporter exporter)
        {
            _exporter = exporter;
        }

        public ImportExportSupportEnum CanExportFile(PackFile file) => _exporter.CanExportFile(file);

        public void Execute(PackFile exportSource, string outputPath, bool generateImporter) 
        {
            _exporter.Export(exportSource.Name, outputPath, SwapBlender);
        }
    }
}
