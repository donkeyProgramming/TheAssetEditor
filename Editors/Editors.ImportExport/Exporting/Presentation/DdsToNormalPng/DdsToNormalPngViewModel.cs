using CommunityToolkit.Mvvm.ComponentModel;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Presentation.DdsToMaterialPng;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.DataTemplates;

namespace Editors.ImportExport.Exporting.Presentation.DdsToNormalPng
{
    internal class DdsToNormalPngViewModel : ObservableObject, IExporterViewModel, IViewProvider
    {
        private readonly DdsToNormalPngExporter _exporter;

        public Type ViewType => typeof(DdsToMaterialPngView);
        public string DisplayName => "Dds_to_MaterialPng";
        public string OutputExtension => ".png";

        public DdsToNormalPngViewModel(DdsToNormalPngExporter exporter)
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
