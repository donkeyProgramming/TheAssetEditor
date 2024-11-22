using CommunityToolkit.Mvvm.ComponentModel;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.DataTemplates;

namespace Editors.ImportExport.Exporting.Presentation.DdsToPng
{
    internal partial class DdsToPngExporterViewModel : ObservableObject, IExporterViewModel, IViewProvider<DdsToPngView>
    {
        private readonly DdsToPngExporter _exporter;

        public string DisplayName => "Dds_to_Png";
        public string OutputExtension => ".png";
        [ObservableProperty] bool _exportTextures = true;
        [ObservableProperty] bool _convertMaterialTextureToBlender = true;
        [ObservableProperty] bool _convertNormalTextureToBlue = true;
        [ObservableProperty] bool _exportAnimations = true;
        public DdsToPngExporterViewModel(DdsToPngExporter exporter)
        {
            _exporter = exporter;
        }

        public ExportSupportEnum CanExportFile(PackFile file) => _exporter.CanExportFile(file);

        public void Execute(PackFile exportSource, string outputPath, bool generateImporter)
        {            
            _exporter.Export(outputPath, exportSource);
        }
    }
}
