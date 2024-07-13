using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters
{

    public interface IExporterViewModel
    {
        public string DisplayName { get; }
        string OutputExtension { get; }

        public void Execute(PackFile exportSource, string outputPath, bool generateImporter);
        public ExportSupportEnum CanExportFile(PackFile file);
    }
}
