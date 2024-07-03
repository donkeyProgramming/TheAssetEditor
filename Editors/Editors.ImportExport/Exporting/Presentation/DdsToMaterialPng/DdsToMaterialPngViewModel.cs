using Editors.ImportExport.Exporting.Exporters;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Presentation.DdsToMaterialPng
{
    internal class DdsToMaterialPngViewModel : IExporterViewModel
    {
        public string DisplayName => "Dds_to_MaterialPng";
        public string OutputExtension => ".png";

        public ExportSupportEnum CanExportFile(PackFile file)
        {
            throw new NotImplementedException();
        }

        public void Execute(string outputPath, bool generateImporter)
        {
            //throw new NotImplementedException();
        }
    }
}
