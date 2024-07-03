using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Presentation.DdsToPng
{
    internal class DdsToPngExporterViewModel : IExporterViewModel
    {
        private readonly DdsToPngExporter _exporter;

        public DdsToPngExporterViewModel(DdsToPngExporter exporter)
        {
            _exporter = exporter;
        }

        public string DisplayName => "Dds_to_Png";

        public string OutputExtension => ".png";

        public ExportSupportEnum CanExportFile(PackFile file)
        {
            throw new NotImplementedException();
        }

        //public IExporter Exporter => throw new NotImplementedException();

        public void Execute(string outputPath, bool generateImporter)
        {
            //throw new NotImplementedException();
        }
    }
}
