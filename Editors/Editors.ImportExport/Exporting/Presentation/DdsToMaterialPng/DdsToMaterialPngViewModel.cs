using CommunityToolkit.Mvvm.ComponentModel;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.DataTemplates;

namespace Editors.ImportExport.Exporting.Presentation.DdsToMaterialPng
{
    internal partial class DdsToMaterialPngViewModel : ObservableObject, IExporterViewModel, IViewProvider<DdsToMaterialPngView>
    {
        public string DisplayName => "Dds_to_MaterialPng";
        public string OutputExtension => ".png";

        [ObservableProperty] bool _swapBlender = true;
        private readonly DdsToMaterialPngExporter _exporter;

        public DdsToMaterialPngViewModel(DdsToMaterialPngExporter exporter)
        {
            _exporter = exporter;
        }

        public ExportSupportEnum CanExportFile(PackFile file) => _exporter.CanExportFile(file);

        public void Execute(string outputPath, bool generateImporter) 
        {
            _exporter.Export(outputPath, SwapBlender);
        }
    }
}
