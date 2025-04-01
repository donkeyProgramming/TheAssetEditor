using CommunityToolkit.Mvvm.ComponentModel;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.DataTemplates;
using System.IO;

namespace Editors.ImportExport.Exporting.Presentation.DdsToNormalPng
{
    internal class DdsToNormalPngViewModel : ObservableObject, IExporterViewModel, IViewProvider<DdsToNormalPngView>
    {
        private readonly IDdsToNormalPngExporter _exporter;

        public string DisplayName => "Dds_to_NormalPng";
        public string OutputExtension => ".png";

        public DdsToNormalPngViewModel(IDdsToNormalPngExporter exporter)
        {
            _exporter = exporter;
        }

        public ExportSupportEnum CanExportFile(PackFile file) => _exporter.CanExportFile(file);

        public void Execute(PackFile exportSource, string outputPath, bool generateImporter)
        {
            _exporter.Export(exportSource.Name, outputPath, true);
        }
    }
}
